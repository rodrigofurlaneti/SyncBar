using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetByAuthId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.GetByAuthId;

public sealed class GetKeetaAuthorizationSessionByAuthIdQueryHandlerTests
{
    private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository = Substitute.For<IKeetaIntegrationAuthorizationSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaAuthorizationSessionByAuthIdQueryHandler _handler;

    public GetKeetaAuthorizationSessionByAuthIdQueryHandlerTests()
    {
        _handler = new GetKeetaAuthorizationSessionByAuthIdQueryHandler(_sessionRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnNotFound()
    {
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(new GetKeetaAuthorizationSessionByAuthIdQuery("auth-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaAuthorizationSession.NotFound");
    }

    [Fact]
    public async Task Handle_SessionFound_ShouldReturnMappedResponse()
    {
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(new GetKeetaAuthorizationSessionByAuthIdQuery("auth-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthId.Should().Be("auth-1");
    }
}
