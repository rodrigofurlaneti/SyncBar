using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.Create;

public sealed class CreateKeetaIntegrationAuthorizationSessionCommandHandlerTests
{
    private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository = Substitute.For<IKeetaIntegrationAuthorizationSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateKeetaIntegrationAuthorizationSessionCommandHandler _handler;

    public CreateKeetaIntegrationAuthorizationSessionCommandHandlerTests()
    {
        _handler = new CreateKeetaIntegrationAuthorizationSessionCommandHandler(_sessionRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_AuthIdAlreadyExists_ShouldReturnConflict()
    {
        var command = new CreateKeetaIntegrationAuthorizationSessionCommand(1, 2, "auth-1", 1);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>())
            .Returns(KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaAuthorizationSession.AlreadyExists");
        await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<KeetaIntegrationAuthorizationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnMappedResponse()
    {
        var command = new CreateKeetaIntegrationAuthorizationSessionCommand(1, 2, "auth-1", 1);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthId.Should().Be("auth-1");
        result.Value.OperationType.Should().Be(1);
        await _sessionRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationAuthorizationSession>(s => s.AuthId == "auth-1"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
