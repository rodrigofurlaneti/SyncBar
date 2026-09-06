using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Authorization;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback;

public sealed class HandleKeetaOAuthCallbackCommandHandlerTests
{
    private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository = Substitute.For<IKeetaIntegrationAuthorizationSessionRepository>();
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly IKeetaAuthClient _authClient = Substitute.For<IKeetaAuthClient>();
    private readonly IKeetaAccessTokenProvider _tokenProvider = Substitute.For<IKeetaAccessTokenProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly HandleKeetaOAuthCallbackCommandHandler _handler;

    public HandleKeetaOAuthCallbackCommandHandlerTests()
    {
        _handler = new HandleKeetaOAuthCallbackCommandHandler(
            _sessionRepository, _mappingRepository, _authClient, _tokenProvider, _logRepository, _unitOfWork);

        _tokenProvider.GetValidAccessTokenAsync(Arg.Any<long>(), Arg.Any<long>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Validation("Keeta.SettingNotConfigured", "sem config")));
    }

    [Fact]
    public async Task Handle_NoExistingSession_ShouldCreateSessionAndCommit()
    {
        var command = new HandleKeetaOAuthCallbackCommand(1, 2, "auth-1", "state-1", null, null);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthId.Should().Be("auth-1");
        await _sessionRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationAuthorizationSession>(s => s.AuthId == "auth-1" && s.CompanyId == 1 && s.BranchId == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSession_ShouldUpdateDetailsInsteadOfCreating()
    {
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        var command = new HandleKeetaOAuthCallbackCommand(1, 2, "auth-1", "new-state", null, "new-code");
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.State.Should().Be("new-state");
        session.AuthorizationCode.Should().Be("new-code");
        await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<KeetaIntegrationAuthorizationSession>(), Arg.Any<CancellationToken>());
        _sessionRepository.Received(1).Update(session);
    }

    [Fact]
    public async Task Handle_TokenResolutionFails_ShouldReturnSuccessWithSessionUnprocessed()
    {
        var command = new HandleKeetaOAuthCallbackCommand(1, 2, "auth-1", null, null, null);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Processed.Should().BeFalse();
        result.Value.MappedKeetaMerchantIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MerchantInfoThrows_ShouldSwallowExceptionAndReturnSuccessUnprocessed()
    {
        var command = new HandleKeetaOAuthCallbackCommand(1, 2, "auth-1", null, null, null);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);
        _tokenProvider.GetValidAccessTokenAsync(1, 2, cancellationToken: Arg.Any<CancellationToken>()).Returns(Result.Success("token-abc"));
        _authClient.GetMerchantInfoAsync(1, 2, "auth-1", "token-abc", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("network down"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Processed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NewAuthorizedShop_ShouldCreateMappingAndMarkSessionProcessed()
    {
        var command = new HandleKeetaOAuthCallbackCommand(1, 2, "auth-1", null, null, null);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);
        _tokenProvider.GetValidAccessTokenAsync(1, 2, cancellationToken: Arg.Any<CancellationToken>()).Returns(Result.Success("token-abc"));

        var shop = new KeetaAuthorizedShop(500, "Loja Teste", "Rua X", 0, 0, "America/Sao_Paulo");
        _authClient.GetMerchantInfoAsync(1, 2, "auth-1", "token-abc", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new KeetaMerchantInfoResponse(1, 1, "Marca", [shop]));
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationMerchantMapping?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Processed.Should().BeTrue();
        result.Value.MappedKeetaMerchantIds.Should().ContainSingle(id => id == 500);
        await _mappingRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationMerchantMapping>(m => m.KeetaMerchantId == 500 && m.StoreName == "Loja Teste"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyMappedShop_ShouldUpdateStatusInsteadOfCreating()
    {
        var command = new HandleKeetaOAuthCallbackCommand(1, 2, "auth-1", null, null, null);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);
        _tokenProvider.GetValidAccessTokenAsync(1, 2, cancellationToken: Arg.Any<CancellationToken>()).Returns(Result.Success("token-abc"));

        var shop = new KeetaAuthorizedShop(500, "Loja Teste", "Rua X", 0, 0, "America/Sao_Paulo");
        _authClient.GetMerchantInfoAsync(1, 2, "auth-1", "token-abc", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new KeetaMerchantInfoResponse(1, 1, "Marca", [shop]));

        var existingMapping = KeetaIntegrationMerchantMapping.Create(1, 2, "500", 500, "Loja Antiga").Value;
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(existingMapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingMapping.IsAuthorized.Should().BeTrue();
        _mappingRepository.Received(1).Update(existingMapping);
        await _mappingRepository.DidNotReceive().AddAsync(Arg.Any<KeetaIntegrationMerchantMapping>(), Arg.Any<CancellationToken>());
    }
}
