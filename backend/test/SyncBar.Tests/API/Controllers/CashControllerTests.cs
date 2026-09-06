using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Cash;
using SyncBar.Application.Features.Cash.CloseSession;
using SyncBar.Application.Features.Cash.GetHistory;
using SyncBar.Application.Features.Cash.GetOpenSession;
using SyncBar.Application.Features.Cash.GetSummary;
using SyncBar.Application.Features.Cash.OpenSession;
using SyncBar.Application.Features.Cash.RegisterMovement;
using SyncBar.Application.Features.Cash.ReviewSession;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class CashControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CashController _controller;

    public CashControllerTests()
    {
        _controller = new CashController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetOpenSession_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetOpenSessionQuery>(q => q.CashRegisterId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((CashSessionResponse)null!));

        var result = await _controller.GetOpenSession(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOpenSession_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetOpenSessionQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CashSessionResponse>(new Error("CashRegister.NotFound", "caixa nao encontrado")));

        var result = await _controller.GetOpenSession(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSummary_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCashSummaryQuery>(q => q.CashSessionId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((CashSummaryResponse)null!));

        var result = await _controller.GetSummary(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSummary_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCashSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CashSummaryResponse>(new Error("CashSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.GetSummary(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetHistory_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCashSessionHistoryQuery>(q => q.BranchId == 1 && q.ReferenceYear == 2026 && q.ReferenceMonth == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<CashSessionHistoryResponse>>([]));

        var result = await _controller.GetHistory(1, 2026, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetHistory_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCashSessionHistoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<CashSessionHistoryResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetHistory(999, 2026, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReviewSession_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<ReviewCashSessionCommand>(c => c.CashSessionId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.ReviewSession(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ReviewSession_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ReviewCashSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CashSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.ReviewSession(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task OpenSession_Success_ShouldReturnCreatedAtActionPointingToGetSummary()
    {
        var command = new OpenCashSessionCommand(1, 1, 100m);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(9L));

        var result = await _controller.OpenSession(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetSummary));
        created.RouteValues!["id"].Should().Be(9L);
    }

    [Fact]
    public async Task OpenSession_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new OpenCashSessionCommand(1, 1, 100m);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("CashRegister.AlreadyExists", "caixa ja aberto")));

        var result = await _controller.OpenSession(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CloseSession_Success_ShouldSendCommandWithIdAndReturnOkWithValue()
    {
        var request = new CloseCashSessionRequest(2, 500m);
        var response = new CloseCashSessionResponse(1, 500m, 500m, 0m);
        _mediator.Send(Arg.Is<CloseCashSessionCommand>(c => c.CashSessionId == 1 && c.ClosedByEmployeeId == 2 && c.ClosingAmount == 500m),
            Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.CloseSession(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task CloseSession_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new CloseCashSessionRequest(2, 500m);
        _mediator.Send(Arg.Any<CloseCashSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CloseCashSessionResponse>(new Error("CashSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.CloseSession(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegisterMovement_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new RegisterCashMovementRequest(1, 1, 50m, "sangria");
        _mediator.Send(Arg.Is<RegisterCashMovementCommand>(c => c.CashSessionId == 1 && c.CashMovementTypeId == 1 && c.EmployeeId == 1 && c.Amount == 50m),
            Arg.Any<CancellationToken>()).Returns(Result.Success(3L));

        var result = await _controller.RegisterMovement(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RegisterMovement_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new RegisterCashMovementRequest(1, 1, 50m, null);
        _mediator.Send(Arg.Any<RegisterCashMovementCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("CashSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.RegisterMovement(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
