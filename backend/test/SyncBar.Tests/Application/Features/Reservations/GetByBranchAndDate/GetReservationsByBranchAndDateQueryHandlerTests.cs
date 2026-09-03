using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Reservations.GetByBranchAndDate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Reservations.GetByBranchAndDate;

public sealed class GetReservationsByBranchAndDateQueryHandlerTests
{
    private readonly ITableReservationRepository _reservationRepository = Substitute.For<ITableReservationRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetReservationsByBranchAndDateQueryHandler _handler;

    public GetReservationsByBranchAndDateQueryHandlerTests()
    {
        _handler = new GetReservationsByBranchAndDateQueryHandler(_reservationRepository, _logRepository, _unitOfWork);
    }

    private static TableReservation CreatePendingReservation(string customerName, DateTime reservedFor)
        => TableReservation.Create(1, null, customerName, "11999998888", 2, reservedFor, "Aniversário").Value;

    [Fact]
    public async Task Handle_NoReservationsInRange_ShouldReturnEmptyCollection()
    {
        var query = new GetReservationsByBranchAndDateQuery(BranchId: 1, From: DateTime.Now, To: DateTime.Now.AddDays(7));
        _reservationRepository.GetByBranchAndDateAsync(query.BranchId, query.From, query.To, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TableReservation>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleReservations_ShouldMapAllFields()
    {
        var query = new GetReservationsByBranchAndDateQuery(BranchId: 1, From: DateTime.Now, To: DateTime.Now.AddDays(7));
        var reservedFor = DateTime.Now.AddDays(2);
        var pending = CreatePendingReservation("Maria Silva", reservedFor);
        var confirmed = CreatePendingReservation("João Souza", reservedFor.AddHours(1));
        confirmed.Confirm(diningTableId: 5);
        _reservationRepository.GetByBranchAndDateAsync(query.BranchId, query.From, query.To, Arg.Any<CancellationToken>())
            .Returns([pending, confirmed]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var responsePending = result.Value.First(r => r.CustomerName == "Maria Silva");
        responsePending.DiningTableId.Should().BeNull();
        responsePending.ReservationStatusId.Should().Be(SyncBar.Domain.Constants.ReservationStatusIds.Pending);
        responsePending.PartySize.Should().Be(2);
        responsePending.Notes.Should().Be("Aniversário");

        var responseConfirmed = result.Value.First(r => r.CustomerName == "João Souza");
        responseConfirmed.DiningTableId.Should().Be(5);
        responseConfirmed.ReservationStatusId.Should().Be(SyncBar.Domain.Constants.ReservationStatusIds.Confirmed);
    }
}
