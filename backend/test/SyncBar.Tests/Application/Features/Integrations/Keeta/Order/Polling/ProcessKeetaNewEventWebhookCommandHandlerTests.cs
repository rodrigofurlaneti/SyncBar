using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Polling;

public sealed class ProcessKeetaNewEventWebhookCommandHandlerTests
{
    private const string ValidPayload = "{\"eventId\":\"evt-1\",\"eventType\":\"CONFIRMED\",\"orderId\":\"order-1\",\"orderURL\":\"https://x\",\"createdAt\":\"2024-01-01T00:00:00Z\"}";
    private const string ValidSignature = "VBC6vqj8NpFkTR0AJ8jmwWZKRGTjZoCKLpYc51l99QA=";

    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly IKeetaCredentialsResolver _credentialsResolver = Substitute.For<IKeetaCredentialsResolver>();
    private readonly IKeetaOrderEventProcessor _eventProcessor = Substitute.For<IKeetaOrderEventProcessor>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ProcessKeetaNewEventWebhookCommandHandler _handler;

    public ProcessKeetaNewEventWebhookCommandHandlerTests()
    {
        _handler = new ProcessKeetaNewEventWebhookCommandHandler(
            _mappingRepository, _credentialsResolver, _eventProcessor, _logRepository, _unitOfWork);
        _eventProcessor.ProcessAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<KeetaPolledEvent>(), Arg.Any<CancellationToken>()).Returns(true);

        _credentialsResolver.ResolveAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com", "client-id", string.Empty, "app-id"));
    }

    private static KeetaIntegrationMerchantMapping MakeMapping() =>
        KeetaIntegrationMerchantMapping.Create(1, 2, "500", 500, "Loja Teste").Value;

    [Fact]
    public async Task Handle_MissingMerchantIdHeader_ShouldReturnInvalidPayload()
    {
        var command = new ProcessKeetaNewEventWebhookCommand(ValidPayload, "app-1", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.InvalidPayload");
    }

    [Fact]
    public async Task Handle_UnknownMerchant_ShouldReturnUnknownMerchant()
    {
        var command = new ProcessKeetaNewEventWebhookCommand(ValidPayload, "app-1", 500, null);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationMerchantMapping?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.UnknownMerchant");
    }

    [Fact]
    public async Task Handle_MissingSignatureWhenSecretConfigured_ShouldReturnMissingSignature()
    {
        var command = new ProcessKeetaNewEventWebhookCommand(ValidPayload, "app-1", 500, null);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(MakeMapping());
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com", "client-id", "test-secret", "app-id"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.MissingSignature");
    }

    [Fact]
    public async Task Handle_InvalidSignatureWhenSecretConfigured_ShouldReturnInvalidSignature()
    {
        var command = new ProcessKeetaNewEventWebhookCommand(ValidPayload, "app-1", 500, "wrong-signature");
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(MakeMapping());
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com", "client-id", "test-secret", "app-id"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.InvalidSignature");
    }

    [Fact]
    public async Task Handle_InvalidJsonPayload_ShouldReturnInvalidPayload()
    {
        var command = new ProcessKeetaNewEventWebhookCommand("not-json", "app-1", 500, null);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(MakeMapping());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.InvalidPayload");
    }

    [Fact]
    public async Task Handle_MissingRequiredFields_ShouldReturnInvalidPayload()
    {
        var command = new ProcessKeetaNewEventWebhookCommand("{\"eventType\":\"CONFIRMED\"}", "app-1", 500, null);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(MakeMapping());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Keeta.InvalidPayload");
    }

    [Fact]
    public async Task Handle_ValidSignatureAndPayload_ShouldProcessEventAndCommit()
    {
        var command = new ProcessKeetaNewEventWebhookCommand(ValidPayload, "app-1", 500, ValidSignature);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(MakeMapping());
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com", "client-id", "test-secret", "app-id"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _eventProcessor.Received(1).ProcessAsync(
            1, 2,
            Arg.Is<KeetaPolledEvent>(e => e.EventId == "evt-1" && e.EventType == "CONFIRMED" && e.OrderId == "order-1"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoClientSecretConfigured_ShouldSkipSignatureValidation()
    {
        var command = new ProcessKeetaNewEventWebhookCommand(ValidPayload, "app-1", 500, null);
        _mappingRepository.GetByKeetaMerchantIdAsync(500, Arg.Any<CancellationToken>()).Returns(MakeMapping());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
