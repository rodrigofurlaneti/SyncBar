using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.CustomerAppUser.Create;
using SyncBar.Application.Features.CustomerAppUser.GetByBranchId;
using SyncBar.Application.Features.CustomerAppUser.GetByCompanyId;
using SyncBar.Application.Features.CustomerAppUser.GetByCustomerId;
using SyncBar.Application.Features.CustomerAppUser.GetById;
using SyncBar.Application.Features.CustomerAppUser.Remove;
using SyncBar.Application.Features.CustomerAppUser.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class CustomerAppUsersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CustomerAppUsersController _controller;

    public CustomerAppUsersControllerTests()
    {
        _controller = new CustomerAppUsersController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomerAppUserByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((SyncBar.Application.Features.CustomerAppUser.GetById.CustomerAppUserResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomerAppUserByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SyncBar.Application.Features.CustomerAppUser.GetById.CustomerAppUserResponse>(new Error("CustomerAppUser.NotFound", "usuario nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomerAppUsersByCustomerIdQuery>(q => q.CustomerId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IEnumerable<SyncBar.Application.Features.CustomerAppUser.GetByCustomerId.CustomerAppUserResponse>>([]));

        var result = await _controller.GetByCustomerId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomerAppUsersByCustomerIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IEnumerable<SyncBar.Application.Features.CustomerAppUser.GetByCustomerId.CustomerAppUserResponse>>(new Error("Customer.NotFound", "cliente nao encontrado")));

        var result = await _controller.GetByCustomerId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomerAppUsersByCompanyIdQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IEnumerable<SyncBar.Application.Features.CustomerAppUser.GetByCompanyId.CustomerAppUserResponse>>([]));

        var result = await _controller.GetByCompanyId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomerAppUsersByCompanyIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IEnumerable<SyncBar.Application.Features.CustomerAppUser.GetByCompanyId.CustomerAppUserResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetByCompanyId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomerAppUsersByBranchIdQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IEnumerable<SyncBar.Application.Features.CustomerAppUser.GetByBranchId.CustomerAppUserResponse>>([]));

        var result = await _controller.GetByBranchId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomerAppUsersByBranchIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IEnumerable<SyncBar.Application.Features.CustomerAppUser.GetByBranchId.CustomerAppUserResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranchId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static CreateCustomerAppUserCommand ValidCreateCommand() => new(
        1, null, null, "12345678900", "jsilva", "j@x.com", "Senha123!");

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithWrappedId()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(9L));

        var result = await _controller.Create(command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { id = 9L });
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("CustomerAppUser.AlreadyExists", "usuario ja existe")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_IdMismatch_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var command = new UpdateCustomerAppUserCommand(2, 1, null, null, "jsilva", "j@x.com", null);

        var result = await _controller.Update(1, command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateCustomerAppUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Success_ShouldReturnNoContent()
    {
        var command = new UpdateCustomerAppUserCommand(1, 1, null, null, "jsilva", "j@x.com", null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new UpdateCustomerAppUserCommand(1, 1, null, null, "jsilva", "j@x.com", null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("CustomerAppUser.NotFound", "usuario nao encontrado")));

        var result = await _controller.Update(1, command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Remove_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<RemoveCustomerAppUserCommand>(c => c.Id == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Remove(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Remove_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RemoveCustomerAppUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerAppUser.NotFound", "usuario nao encontrado")));

        var result = await _controller.Remove(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
