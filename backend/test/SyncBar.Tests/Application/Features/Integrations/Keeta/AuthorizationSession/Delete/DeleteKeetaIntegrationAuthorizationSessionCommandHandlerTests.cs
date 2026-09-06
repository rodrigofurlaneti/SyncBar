using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.Delete;

public sealed class DeleteKeetaIntegrationAuthorizationSessionCommandHandlerTests
{
    private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository = Substitute.For<IKeetaIntegrationAuthorizationSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteKeetaIntegrationAuthorizationSessionCommandHandler _handler;

    public DeleteKeetaIntegrationAuthorizationSessionCommandHandlerTests()
    {
        _handler = new DeleteKeetaIntegrationAuthorizationSessionCommandHandler(_sessionRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnNotFound()
    {
        var command = new DeleteKeetaIntegrationAuthorizationSessionCommand(1, 1);
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaAuthorizationSession.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        var command = new DeleteKeetaIntegrationAuthorizationSessionCommand(1, 2);
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaAuthorizationSession.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteAndCommit()
    {
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        var command = new DeleteKeetaIntegrationAuthorizationSessionCommand(1, 1);
        _sessionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _sessionRepository.Received(1).Delete(session);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
