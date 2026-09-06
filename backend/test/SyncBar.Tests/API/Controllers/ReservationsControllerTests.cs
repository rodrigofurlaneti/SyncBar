using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Reservations;
using SyncBar.Application.Features.Reservations.Cancel;
using SyncBar.Application.Features.Reservations.Confirm;
using SyncBar.Application.Features.Reservations.Create;
using SyncBar.Application.Features.Reservations.GetByBranchAndDate;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class ReservationsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ReservationsController _controller;

    public ReservationsControllerTests()
    {
        _controller = new ReservationsController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByBranchAndDate_Success_ShouldForwardRangeAndReturnOk()
    {
        var from = DateTime.Today;
        var to = DateTime.Today.AddDays(1);
        _mediator.Send(Arg.Is<GetReservationsByBranchAndDateQuery>(q => q.BranchId == 1 && q.From == from && q.To == to), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<ReservationResponse>>([]));

        var result = await _controller.GetByBranchAndDate(1, from, to, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranchAndDate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetReservationsByBranchAndDateQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<ReservationResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranchAndDate(999, DateTime.Today, DateTime.Today, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static CreateReservationCommand ValidCommand() => new(1, "Cliente Teste", null, 4, DateTime.Today.AddHours(20), null);

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithValue()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(5L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Confirm_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new ConfirmReservationRequest(2);
        _mediator.Send(Arg.Is<ConfirmReservationCommand>(c => c.ReservationId == 1 && c.DiningTableId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Confirm(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Confirm_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new ConfirmReservationRequest(2);
        _mediator.Send(Arg.Any<ConfirmReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Reservation.NotFound", "reserva nao encontrada")));

        var result = await _controller.Confirm(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Cancel_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<CancelReservationCommand>(c => c.ReservationId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Cancel(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Cancel_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CancelReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Reservation.NotFound", "reserva nao encontrada")));

        var result = await _controller.Cancel(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
