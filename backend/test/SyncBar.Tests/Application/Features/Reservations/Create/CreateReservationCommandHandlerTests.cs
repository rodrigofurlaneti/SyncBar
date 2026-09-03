using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Reservations.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Reservations.Create;

public sealed class CreateReservationCommandHandlerTests
{
    private readonly ITableReservationRepository _reservationRepository = Substitute.For<ITableReservationRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateReservationCommandHandler _handler;

    public CreateReservationCommandHandlerTests()
    {
        _handler = new CreateReservationCommandHandler(_reservationRepository, _logRepository, _unitOfWork);
    }

    private static CreateReservationCommand CreateValidCommand(
        string customerName = "Maria Silva", int partySize = 4, DateTime? reservedFor = null)
        => new(
            BranchId: 1,
            CustomerName: customerName,
            CustomerPhone: "11999998888",
            PartySize: partySize,
            ReservedFor: reservedFor ?? DateTime.Now.AddDays(1),
            Notes: "Mesa perto da janela");

    [Fact]
    public async Task Handle_EmptyCustomerName_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(customerName: "");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReservation.EmptyCustomerName");
        await _reservationRepository.DidNotReceive().AddAsync(Arg.Any<TableReservation>(), Arg.Any<CancellationToken>());
        // Sem persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PartySizeZeroOrNegative_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(partySize: 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReservation.InvalidPartySize");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReservedForInThePast_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(reservedFor: DateTime.Now.AddDays(-1));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReservation.PastDate");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistReservationAndReturnItsId()
    {
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _reservationRepository.Received(1).AddAsync(
            Arg.Is<TableReservation>(r =>
                r.BranchId == command.BranchId &&
                r.DiningTableId == null &&
                r.CustomerName == command.CustomerName &&
                r.CustomerPhone == command.CustomerPhone &&
                r.PartySize == command.PartySize &&
                r.ReservedFor == command.ReservedFor &&
                r.Notes == command.Notes),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
