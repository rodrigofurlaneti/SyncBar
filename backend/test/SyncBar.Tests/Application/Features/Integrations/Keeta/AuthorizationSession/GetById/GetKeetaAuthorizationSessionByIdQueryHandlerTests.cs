using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.GetById;

public sealed class GetKeetaAuthorizationSessionByIdQueryHandlerTests
{
    private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository = Substitute.For<IKeetaIntegrationAuthorizationSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaAuthorizationSessionByIdQueryHandler _handler;

    public GetKeetaAuthorizationSessionByIdQueryHandlerTests()
    {
        _handler = new GetKeetaAuthorizationSessionByIdQueryHandler(_sessionRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnNotFound()
    {
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(new GetKeetaAuthorizationSessionByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaAuthorizationSession.NotFound");
    }

    [Fact]
    public async Task Handle_SessionFound_ShouldReturnMappedResponse()
    {
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(new GetKeetaAuthorizationSessionByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthId.Should().Be("auth-1");
        result.Value.IsProcessed.Should().BeFalse();
    }
}
