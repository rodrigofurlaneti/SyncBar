using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent;

public sealed class HasAlreadyProcessedEventQueryHandlerTests
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly HasAlreadyProcessedEventQueryHandler _handler;

    public HasAlreadyProcessedEventQueryHandlerTests()
    {
        _handler = new HasAlreadyProcessedEventQueryHandler(_webhookLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EventAlreadyProcessed_ShouldReturnTrue()
    {
        _webhookLogRepository.HasAlreadyProcessedEventAsync("evt-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new HasAlreadyProcessedEventQuery("evt-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EventNotProcessedYet_ShouldReturnFalse()
    {
        _webhookLogRepository.HasAlreadyProcessedEventAsync("evt-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new HasAlreadyProcessedEventQuery("evt-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
