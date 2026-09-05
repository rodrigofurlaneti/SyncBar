using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs;

public sealed class GetUnprocessedAsaasWebhookLogsQueryHandlerTests
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetUnprocessedAsaasWebhookLogsQueryHandler _handler;

    public GetUnprocessedAsaasWebhookLogsQueryHandlerTests()
    {
        _handler = new GetUnprocessedAsaasWebhookLogsQueryHandler(_webhookLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoUnprocessedLogs_ShouldReturnEmptyList()
    {
        _webhookLogRepository.GetUnprocessedLogsAsync(1, 50, Arg.Any<CancellationToken>())
            .Returns(new List<AsaasIntegrationWebhookLog>());

        var result = await _handler.Handle(new GetUnprocessedAsaasWebhookLogsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HasUnprocessedLogs_ShouldReturnMappedListRespectingLimit()
    {
        var logs = new List<AsaasIntegrationWebhookLog>
        {
            AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_CREATED", "evt-1", "pay_1", "{}").Value,
        };
        _webhookLogRepository.GetUnprocessedLogsAsync(1, 10, Arg.Any<CancellationToken>()).Returns(logs);

        var result = await _handler.Handle(new GetUnprocessedAsaasWebhookLogsQuery(1, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        await _webhookLogRepository.Received(1).GetUnprocessedLogsAsync(1, 10, Arg.Any<CancellationToken>());
    }
}
