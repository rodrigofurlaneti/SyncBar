using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetPendingSessions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.GetPendingSessions;

public sealed class GetPendingKeetaAuthorizationSessionsQueryHandlerTests
{
    private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository = Substitute.For<IKeetaIntegrationAuthorizationSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPendingKeetaAuthorizationSessionsQueryHandler _handler;

    public GetPendingKeetaAuthorizationSessionsQueryHandlerTests()
    {
        _handler = new GetPendingKeetaAuthorizationSessionsQueryHandler(_sessionRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_PendingSessionsExist_ShouldReturnMappedList()
    {
        var sessions = new List<KeetaIntegrationAuthorizationSession>
        {
            KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value,
            KeetaIntegrationAuthorizationSession.Create(1, 3, "auth-2", 1).Value,
        };
        _sessionRepository.GetPendingSessionsAsync(Arg.Any<CancellationToken>()).Returns(sessions);

        var result = await _handler.Handle(new GetPendingKeetaAuthorizationSessionsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoPendingSessions_ShouldReturnEmptyList()
    {
        _sessionRepository.GetPendingSessionsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetPendingKeetaAuthorizationSessionsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
