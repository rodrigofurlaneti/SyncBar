using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.GetById;

public sealed class GetAsaasWebhookLogByIdQueryHandlerTests
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasWebhookLogByIdQueryHandler _handler;

    public GetAsaasWebhookLogByIdQueryHandlerTests()
    {
        _handler = new GetAsaasWebhookLogByIdQueryHandler(_webhookLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_LogNotFound_ShouldReturnNotFound()
    {
        _webhookLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationWebhookLog?)null);

        var result = await _handler.Handle(new GetAsaasWebhookLogByIdQuery(1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;
        _webhookLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new GetAsaasWebhookLogByIdQuery(1, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.NotFound");
    }

    [Fact]
    public async Task Handle_LogFound_ShouldReturnMappedResponseWithStringStatus()
    {
        var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;
        _webhookLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new GetAsaasWebhookLogByIdQuery(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Pending");
        result.Value.Event.Should().Be("PAYMENT_RECEIVED");
    }
}
