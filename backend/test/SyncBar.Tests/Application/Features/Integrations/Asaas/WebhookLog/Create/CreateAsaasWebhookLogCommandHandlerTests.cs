using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.Create;

public sealed class CreateAsaasWebhookLogCommandHandlerTests
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateAsaasWebhookLogCommandHandler _handler;

    public CreateAsaasWebhookLogCommandHandlerTests()
    {
        _handler = new CreateAsaasWebhookLogCommandHandler(_webhookLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_DuplicateEventId_ShouldReturnFailure()
    {
        var command = new CreateAsaasWebhookLogCommand(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}", null, null);
        _webhookLogRepository.ExistsByEventIdAsync("evt-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.DuplicateEvent");
    }

    [Fact]
    public async Task Handle_NoEventIdProvided_ShouldSkipDuplicateCheckAndPersist()
    {
        var command = new CreateAsaasWebhookLogCommand(1, null, "PAYMENT_RECEIVED", null, "pay_1", "{}", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _webhookLogRepository.DidNotReceive().ExistsByEventIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyPayload_ShouldReturnDomainValidationFailure()
    {
        var command = new CreateAsaasWebhookLogCommand(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "  ", null, null);
        _webhookLogRepository.ExistsByEventIdAsync("evt-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payload.Empty");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnMappedResponse()
    {
        var command = new CreateAsaasWebhookLogCommand(1, 2, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{\"event\":\"PAYMENT_RECEIVED\"}", "{}", "127.0.0.1");
        _webhookLogRepository.ExistsByEventIdAsync("evt-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Event.Should().Be("PAYMENT_RECEIVED");
        result.Value.PaymentId.Should().Be("pay_1");
        await _webhookLogRepository.Received(1).AddAsync(
            Arg.Is<AsaasIntegrationWebhookLog>(l => l.AsaasEventId == "evt-1" && l.CompanyId == 1),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
