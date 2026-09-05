using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;

public sealed class GetAsaasWebhookLogsByPaymentIdQueryHandlerTests
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasWebhookLogsByPaymentIdQueryHandler _handler;

    public GetAsaasWebhookLogsByPaymentIdQueryHandlerTests()
    {
        _handler = new GetAsaasWebhookLogsByPaymentIdQueryHandler(_webhookLogRepository, _logRepository, _unitOfWork);
    }

    [Fact(Skip = "Este teste está suspenso até que o bug #123 seja corrigido.")]
    public async Task Handle_NoLogsForPayment_ShouldReturnEmptyList()
    {
        _webhookLogRepository.GetByPaymentIdAsync(1, "pay_1", Arg.Any<CancellationToken>())
            .Returns(new List<AsaasIntegrationWebhookLog>());

        var result = await _handler.Handle(new GetAsaasWebhookLogsByPaymentIdQuery(1, "pay_1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HasLogs_ShouldReturnMappedList()
    {
        var logs = new List<AsaasIntegrationWebhookLog>
        {
            AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_CREATED", "evt-1", "pay_1", "{}").Value,
            AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-2", "pay_1", "{}").Value,
        };
        _webhookLogRepository.GetByPaymentIdAsync(1, "pay_1", Arg.Any<CancellationToken>()).Returns(logs);

        var result = await _handler.Handle(new GetAsaasWebhookLogsByPaymentIdQuery(1, "pay_1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
