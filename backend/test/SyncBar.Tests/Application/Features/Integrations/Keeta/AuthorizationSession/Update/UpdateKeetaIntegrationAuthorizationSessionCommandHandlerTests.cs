using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.Update;

public sealed class UpdateKeetaIntegrationAuthorizationSessionCommandHandlerTests
{
    private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository = Substitute.For<IKeetaIntegrationAuthorizationSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateKeetaIntegrationAuthorizationSessionCommandHandler _handler;

    public UpdateKeetaIntegrationAuthorizationSessionCommandHandlerTests()
    {
        _handler = new UpdateKeetaIntegrationAuthorizationSessionCommandHandler(_sessionRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateKeetaIntegrationAuthorizationSessionCommand(1, 1, MarkAsProcessed: true);
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaAuthorizationSession.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        var command = new UpdateKeetaIntegrationAuthorizationSessionCommand(1, 2, MarkAsProcessed: true);
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaAuthorizationSession.NotFound");
    }

    [Fact]
    public async Task Handle_UpdateDetails_ShouldApplyAndCommit()
    {
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        var command = new UpdateKeetaIntegrationAuthorizationSessionCommand(1, 1, KeetaMerchantId: 500, AuthorizationCode: "code-1", State: "state-1");
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.KeetaMerchantId.Should().Be(500);
        session.AuthorizationCode.Should().Be("code-1");
        session.State.Should().Be("state-1");
        _sessionRepository.Received(1).Update(session);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MarkAsProcessed_ShouldSetIsProcessedTrue()
    {
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        var command = new UpdateKeetaIntegrationAuthorizationSessionCommand(1, 1, MarkAsProcessed: true);
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.IsProcessed.Should().BeTrue();
    }
}
