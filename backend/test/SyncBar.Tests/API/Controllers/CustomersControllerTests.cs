using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Customers;
using SyncBar.Application.Features.Customers.AddLoyaltyPoints;
using SyncBar.Application.Features.Customers.Create;
using SyncBar.Application.Features.Customers.GetByCompany;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class CustomersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        _controller = new CustomersController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByCompany_Success_ShouldForwardSearchTermAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomersByCompanyQuery>(q => q.CompanyId == 1 && q.Search == "joao"), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<CustomerResponse>>([]));

        var result = await _controller.GetByCompany(1, "joao", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomersByCompanyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<CustomerResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetByCompany(999, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithValue()
    {
        var command = new CreateCustomerCommand(1, "Cliente Teste", null, null, null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(5L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateCustomerCommand(1, "", null, null, null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Customer.EmptyName", "nome obrigatorio")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddLoyaltyPoints_Success_ShouldSendCommandWithIdAndPointsAndReturnNoContent()
    {
        var request = new AddLoyaltyPointsRequest(10);
        _mediator.Send(Arg.Is<AddLoyaltyPointsCommand>(c => c.CustomerId == 1 && c.Points == 10), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.AddLoyaltyPoints(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddLoyaltyPoints_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddLoyaltyPointsRequest(10);
        _mediator.Send(Arg.Any<AddLoyaltyPointsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Customer.NotFound", "cliente nao encontrado")));

        var result = await _controller.AddLoyaltyPoints(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
