using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.ProcessAuthorizationWebhook;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Authorization.ProcessAuthorizationWebhook;

public sealed class ProcessKeetaAuthorizationWebhookCommandHandlerTests
{
    // Payload e assinatura Base64 pré-computados (HMACSHA256, chave "test-secret") — usados para
    // validar o caminho de assinatura válida sem depender de gerar o hash em runtime no teste.
    private const string ValidPayload = "{\"clientId\":123,\"authId\":\"auth-1\",\"opType\":1,\"shopId\":500,\"shopName\":\"Loja Teste\",\"createTime\":1700000000000}";
    private const string ValidSignature = "qh2mRcEwV2el3APtJ8FXyp/BvZ9u/sX7i1VzbgO9KO8=";
    private const string CancelPayload = "{\"clientId\":123,\"authId\":\"auth-1\",\"opType\":2,\"shopId\":500,\"shopName\":\"Loja Teste\",\"createTime\":1700000000000}";

    private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository = Substitute.For<IKeetaIntegrationAuthorizationSessionRepository>();
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly IKeetaCredentialsResolver _credentialsResolver = Substitute.For<IKeetaCredentialsResolver>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ProcessKeetaAuthorizationWebhookCommandHandler _handler;

    public ProcessKeetaAuthorizationWebhookCommandHandlerTests()
    {
        _handler = new ProcessKeetaAuthorizationWebhookCommandHandler(
            _sessionRepository, _mappingRepository, _credentialsResolver, _logRepository, _unitOfWork);

        _credentialsResolver.ResolveDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com", "client-id", string.Empty, "app-id"));
    }

    [Fact]
    public async Task Handle_InvalidJsonPayload_ShouldReturnInvalidPayload()
    {
        var command = new ProcessKeetaAuthorizationWebhookCommand("not-json", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.InvalidPayload");
    }

    [Fact]
    public async Task Handle_MissingAuthId_ShouldReturnInvalidPayload()
    {
        var command = new ProcessKeetaAuthorizationWebhookCommand("{\"opType\":1}", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.InvalidPayload");
    }

    [Fact]
    public async Task Handle_NoClientSecretConfigured_ShouldSkipSignatureValidation()
    {
        var command = new ProcessKeetaAuthorizationWebhookCommand(ValidPayload, null);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MissingSignatureWhenSecretConfigured_ShouldReturnMissingSignature()
    {
        _credentialsResolver.ResolveDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com", "client-id", "test-secret", "app-id"));
        var command = new ProcessKeetaAuthorizationWebhookCommand(ValidPayload, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.MissingSignature");
    }

    [Fact]
    public async Task Handle_InvalidSignatureWhenSecretConfigured_ShouldReturnInvalidSignature()
    {
        _credentialsResolver.ResolveDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com", "client-id", "test-secret", "app-id"));
        var command = new ProcessKeetaAuthorizationWebhookCommand(ValidPayload, "wrong-signature");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.InvalidSignature");
    }

    [Fact]
    public async Task Handle_ValidSignature_OpType1_NoSessionYet_ShouldReturnSuccessWithoutPersisting()
    {
        _credentialsResolver.ResolveDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com", "client-id", "test-secret", "app-id"));
        var command = new ProcessKeetaAuthorizationWebhookCommand(ValidPayload, ValidSignature);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mappingRepository.DidNotReceive().AddAsync(Arg.Any<KeetaIntegrationMerchantMapping>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OpType1_SessionExists_NoMappingYet_ShouldCreateMappingAndMarkProcessed()
    {
        var command = new ProcessKeetaAuthorizationWebhookCommand(ValidPayload, null);
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns(session);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationMerchantMapping?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.IsProcessed.Should().BeTrue();
        await _mappingRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationMerchantMapping>(m => m.KeetaMerchantId == 500 && m.StoreName == "Loja Teste"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OpType1_SessionExists_MappingAlreadyExists_ShouldUpdateStatus()
    {
        var command = new ProcessKeetaAuthorizationWebhookCommand(ValidPayload, null);
        var session = KeetaIntegrationAuthorizationSession.Create(1, 2, "auth-1", 1).Value;
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns(session);

        var existingMapping = KeetaIntegrationMerchantMapping.Create(1, 2, "500", 500, "Loja Antiga").Value;
        existingMapping.UpdateStatus(isAuthorized: false, isOnboarded: false);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(existingMapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingMapping.IsAuthorized.Should().BeTrue();
        _mappingRepository.Received(1).Update(existingMapping);
    }

    [Fact]
    public async Task Handle_OpType2_MappingExists_ShouldRevokeAuthorization()
    {
        var command = new ProcessKeetaAuthorizationWebhookCommand(CancelPayload, null);
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "500", 500, "Loja Teste").Value;
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(mapping);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mapping.IsAuthorized.Should().BeFalse();
        _mappingRepository.Received(1).Update(mapping);
    }

    [Fact]
    public async Task Handle_OpType2_NoMappingFound_ShouldStillSucceed()
    {
        var command = new ProcessKeetaAuthorizationWebhookCommand(CancelPayload, null);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationMerchantMapping?)null);
        _sessionRepository.GetByAuthIdAsync("auth-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationAuthorizationSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
