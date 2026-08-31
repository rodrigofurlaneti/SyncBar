using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash.OpenSession;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.OpenSession;

public sealed class OpenCashSessionCommandHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly OpenCashSessionCommandHandler _handler;

    public OpenCashSessionCommandHandlerTests()
    {
        _handler = new OpenCashSessionCommandHandler(_cashSessionRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_CashRegisterAlreadyHasOpenSession_ShouldReturnFailureWithoutPersistingNewSession()
    {
        var command = new OpenCashSessionCommand(CashRegisterId: 1, OpenedByEmployeeId: 10, OpeningAmount: 100m);
        var existingOpenSession = CashSession.Open(command.CashRegisterId, 99, 50m).Value;
        _cashSessionRepository.GetOpenByCashRegisterAsync(command.CashRegisterId, Arg.Any<CancellationToken>())
            .Returns(existingOpenSession);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.AlreadyOpen");

        await _cashSessionRepository.DidNotReceive().AddAsync(Arg.Any<CashSession>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeOpeningAmount_ShouldReturnFailureWithoutPersisting()
    {
        var command = new OpenCashSessionCommand(CashRegisterId: 1, OpenedByEmployeeId: 10, OpeningAmount: -1m);
        _cashSessionRepository.GetOpenByCashRegisterAsync(command.CashRegisterId, Arg.Any<CancellationToken>())
            .Returns((CashSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.InvalidOpeningAmount");

        await _cashSessionRepository.DidNotReceive().AddAsync(Arg.Any<CashSession>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistNewSessionAndReturnItsId()
    {
        var command = new OpenCashSessionCommand(CashRegisterId: 1, OpenedByEmployeeId: 10, OpeningAmount: 200m);
        _cashSessionRepository.GetOpenByCashRegisterAsync(command.CashRegisterId, Arg.Any<CancellationToken>())
            .Returns((CashSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _cashSessionRepository.Received(1).AddAsync(
            Arg.Is<CashSession>(s =>
                s.CashRegisterId == command.CashRegisterId &&
                s.OpenedByEmployeeId == command.OpenedByEmployeeId &&
                s.OpeningAmount == command.OpeningAmount &&
                s.IsActive &&
                s.IsOpen()),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
