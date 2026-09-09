using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.CustomerAddresses.Create;
using SyncBar.Application.Features.CustomerAddresses.GetByCustomerId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class StorefrontCustomerAddressesControllerTests
{
    private readonly IMediator mediator = Substitute.For<IMediator>();
    private readonly IBranchRepository branches = Substitute.For<IBranchRepository>();
    private StorefrontCustomerAddressesController Controller(bool authenticated = true)
    {
        var http = new DefaultHttpContext();
        if (authenticated) http.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("customerId", "901"), new Claim("companyId", "42"),
            new Claim(ClaimTypes.NameIdentifier, "123"), new Claim(ClaimTypes.Role, "Customer")], "test"));
        return new(mediator, branches) { ControllerContext = new ControllerContext { HttpContext = http } };
    }

    [Theory]
    [InlineData(902, true)]
    [InlineData(901, false)]
    public async Task CannotReadAnotherCustomerOrWithoutCustomerIdentity(long customerId, bool authenticated)
    {
        (await Controller(authenticated).Get(customerId, default)).Should().BeOfType<ForbidResult>();
        await mediator.DidNotReceive().Send(Arg.Any<GetCustomerAddressesByCustomerIdQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUsesCustomerAndCompanyClaims()
    {
        branches.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Branch.Create(42, "Filial", null, null, null, null, null, null, null, null).Value);
        mediator.Send(Arg.Is<CreateCustomerAddressCommand>(c => c.CustomerId == 901 && c.CompanyId == 42 && c.BranchId == 7), Arg.Any<CancellationToken>())
            .Returns(Result.Success(10L));
        var result = await Controller().Create(new(7, "Rua Teste", "1", null, "01001000"), default);
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new { id = 10L });
    }

    [Fact]
    public async Task CannotCreateAddressForAnotherCompanyBranch()
    {
        branches.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Branch.Create(99, "Outra", null, null, null, null, null, null, null, null).Value);
        (await Controller().Create(new(7, "Rua Teste", "1", null, "01001000"), default)).Should().BeOfType<ForbidResult>();
        await mediator.DidNotReceive().Send(Arg.Any<CreateCustomerAddressCommand>(), Arg.Any<CancellationToken>());
    }
}
