using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Enums;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.Update;

public sealed class UpdateAsaasWebhookLogStatusCommandHandlerTests
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateAsaasWebhookLogStatusCommandHandler _handler;

    public UpdateAsaasWebhookLogStatusCommandHandlerTests()
    {
        _handler = new UpdateAsaasWebhookLogStatusCommandHandler(_webhookLogRepository, _logRepository, _unitOfWork);
    }

    private static AsaasIntegrationWebhookLog CreateLog() =>
        AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;

    [Fact]
    public async Task Handle_LogNotFound_ShouldReturnNotFound()
    {
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationWebhookLog?)null);

        var result = await _handler.Handle(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Processed), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var log = CreateLog();
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new UpdateAsaasWebhookLogStatusCommand(1, 2, WebhookLogStatus.Processed), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.NotFound");
    }

    [Fact]
    public async Task Handle_MarkAsProcessed_ShouldUpdateStatusAndCommit()
    {
        var log = CreateLog();
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Processed), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        log.Status.Should().Be(WebhookLogStatus.Processed);
        _webhookLogRepository.Received(1).Update(log);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MarkAsProcessedTwice_ShouldReturnDomainFailure()
    {
        var log = CreateLog();
        log.MarkAsProcessed();
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Processed), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WebhookLog.AlreadyProcessed");
    }

    [Fact]
    public async Task Handle_MarkAsFailed_ShouldSetErrorMessage()
    {
        var log = CreateLog();
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(
            new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Failed, "pedido nao encontrado"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        log.Status.Should().Be(WebhookLogStatus.Failed);
        log.ErrorMessage.Should().Be("pedido nao encontrado");
    }

    [Fact]
    public async Task Handle_MarkAsFailedWithoutErrorMessage_ShouldUseDefaultMessage()
    {
        var log = CreateLog();
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Failed), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        log.ErrorMessage.Should().Be("Erro desconhecido durante o processamento do webhook.");
    }

    [Fact]
    public async Task Handle_InvalidStatusTransition_ShouldReturnValidationFailure()
    {
        var log = CreateLog();
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Pending), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WebhookLog.InvalidStatus");
        _webhookLogRepository.DidNotReceive().Update(Arg.Any<AsaasIntegrationWebhookLog>());
    }
}
