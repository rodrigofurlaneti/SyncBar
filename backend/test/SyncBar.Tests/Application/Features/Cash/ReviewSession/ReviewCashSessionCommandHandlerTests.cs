using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash.ReviewSession;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.ReviewSession;

public sealed class ReviewCashSessionCommandHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ReviewCashSessionCommandHandler _handler;

    public ReviewCashSessionCommandHandlerTests()
    {
        _handler = new ReviewCashSessionCommandHandler(_cashSessionRepository, _logRepository, _unitOfWork);
    }

    private static CashSession CreateOpenSession(decimal openingAmount = 100m)
        => CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount).Value;

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnFailure()
    {
        var command = new ReviewCashSessionCommand(CashSessionId: 1);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns((CashSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionInactive_ShouldReturnFailure()
    {
        var session = CreateOpenSession();
        session.Deactivate();
        var command = new ReviewCashSessionCommand(CashSessionId: 1);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionStillOpen_ShouldReturnFailureWithoutExplicitCommit()
    {
        // Sessão ativa mas ainda aberta (não fechada) -> MarkAsReviewed deve recusar.
        var session = CreateOpenSession();
        var command = new ReviewCashSessionCommand(CashSessionId: 1);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotClosed");
        session.CashSessionStatusId.Should().Be(CashSessionStatusIds.Aberto);
        // Sem commit explícito nesse ramo: só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClosedSession_ShouldMarkAsReviewedAndCommit()
    {
        var session = CreateOpenSession();
        session.Close(closedByEmployeeId: 10, closingAmount: 100m, expectedAmount: 100m);
        var command = new ReviewCashSessionCommand(CashSessionId: 1);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.CashSessionStatusId.Should().Be(CashSessionStatusIds.Conferido);
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
