using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash.RegisterMovement;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.RegisterMovement;

public sealed class RegisterCashMovementCommandHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ICashMovementRepository _cashMovementRepository = Substitute.For<ICashMovementRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RegisterCashMovementCommandHandler _handler;

    public RegisterCashMovementCommandHandlerTests()
    {
        _handler = new RegisterCashMovementCommandHandler(
            _cashSessionRepository, _cashMovementRepository, _logRepository, _unitOfWork);
    }

    private static CashSession CreateOpenSession(decimal openingAmount = 100m)
        => CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount).Value;

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnFailureWithoutPersisting()
    {
        var command = new RegisterCashMovementCommand(
            CashSessionId: 1, CashMovementTypeId: CashMovementTypeIds.Suprimento, EmployeeId: 10, Amount: 50m, Description: "Reforço");
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns((CashSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");

        await _cashMovementRepository.DidNotReceive().AddAsync(Arg.Any<CashMovement>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionInactive_ShouldReturnFailureWithoutPersisting()
    {
        var session = CreateOpenSession();
        session.Deactivate();
        var command = new RegisterCashMovementCommand(
            CashSessionId: 1, CashMovementTypeId: CashMovementTypeIds.Suprimento, EmployeeId: 10, Amount: 50m, Description: null);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");
        await _cashMovementRepository.DidNotReceive().AddAsync(Arg.Any<CashMovement>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionNotOpen_ShouldReturnFailureWithoutPersisting()
    {
        // Sessão ativa, mas já fechada -> IsOpen() é falso.
        var session = CreateOpenSession();
        session.Close(closedByEmployeeId: 10, closingAmount: 100m, expectedAmount: 100m);
        var command = new RegisterCashMovementCommand(
            CashSessionId: 1, CashMovementTypeId: CashMovementTypeIds.Sangria, EmployeeId: 10, Amount: 20m, Description: "Sangria");
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
        await _cashMovementRepository.DidNotReceive().AddAsync(Arg.Any<CashMovement>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistMovementAndReturnItsId()
    {
        var session = CreateOpenSession();
        var command = new RegisterCashMovementCommand(
            CashSessionId: 1, CashMovementTypeId: CashMovementTypeIds.Suprimento, EmployeeId: 10, Amount: 75m, Description: "Reforço de troco");
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _cashMovementRepository.Received(1).AddAsync(
            Arg.Is<CashMovement>(m =>
                m.CashSessionId == command.CashSessionId &&
                m.CashMovementTypeId == command.CashMovementTypeId &&
                m.SaleId == null &&
                m.EmployeeId == command.EmployeeId &&
                m.Amount == command.Amount &&
                m.Description == command.Description &&
                m.IsActive),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
