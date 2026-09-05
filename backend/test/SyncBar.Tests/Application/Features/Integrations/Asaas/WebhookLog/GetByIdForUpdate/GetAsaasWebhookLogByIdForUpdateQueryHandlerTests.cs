using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByIdForUpdate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.GetByIdForUpdate;

public sealed class GetAsaasWebhookLogByIdForUpdateQueryHandlerTests
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasWebhookLogByIdForUpdateQueryHandler _handler;

    public GetAsaasWebhookLogByIdForUpdateQueryHandlerTests()
    {
        _handler = new GetAsaasWebhookLogByIdForUpdateQueryHandler(_webhookLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_LogNotFound_ShouldReturnNotFound()
    {
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationWebhookLog?)null);

        var result = await _handler.Handle(new GetAsaasWebhookLogByIdForUpdateQuery(1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new GetAsaasWebhookLogByIdForUpdateQuery(1, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.NotFound");
    }

    [Fact]
    public async Task Handle_LogFound_ShouldReturnMappedResponse()
    {
        var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new GetAsaasWebhookLogByIdForUpdateQuery(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AsaasEventId.Should().Be("evt-1");
    }
}
