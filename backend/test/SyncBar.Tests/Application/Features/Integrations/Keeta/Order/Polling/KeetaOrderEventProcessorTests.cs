using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Polling;

public sealed class KeetaOrderEventProcessorTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository = Substitute.For<IKeetaIntegrationOrderEventLogRepository>();

    private readonly KeetaOrderEventProcessor _processor;

    public KeetaOrderEventProcessorTests()
    {
        _processor = new KeetaOrderEventProcessor(_orderRepository, _eventLogRepository);
    }

    private static KeetaIntegrationOrder MakeOrder() =>
        KeetaIntegrationOrder.Create(1, 2, 3, 4, "order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task ProcessAsync_EventAlreadyLogged_ShouldSkipAndReturnTrue()
    {
        _eventLogRepository.ExistsByEventIdAsync("evt-1", Arg.Any<CancellationToken>()).Returns(true);
        var polledEvent = new KeetaPolledEvent("evt-1", "CONFIRMED", "order-1", "https://x", DateTime.UtcNow, "{}");

        var result = await _processor.ProcessAsync(1, 2, polledEvent, CancellationToken.None);

        result.Should().BeTrue();
        await _eventLogRepository.DidNotReceive().AddAsync(Arg.Any<KeetaIntegrationOrderEventLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_CreatedEventWithNoLocalOrder_ShouldLogAsFailedAndReturnFalse()
    {
        _eventLogRepository.ExistsByEventIdAsync("evt-1", Arg.Any<CancellationToken>()).Returns(false);
        _orderRepository.GetByKeetaOrderIdAsync("order-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);
        var polledEvent = new KeetaPolledEvent("evt-1", "CREATED", "order-1", "https://x", DateTime.UtcNow, "{}");

        var result = await _processor.ProcessAsync(1, 2, polledEvent, CancellationToken.None);

        result.Should().BeFalse();
        await _eventLogRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationOrderEventLog>(l => !l.ProcessedSuccessfully && l.ErrorMessage!.Contains("pipeline de criação")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_NonCreatedEventWithNoLocalOrder_ShouldLogDifferentFailureReason()
    {
        _eventLogRepository.ExistsByEventIdAsync("evt-1", Arg.Any<CancellationToken>()).Returns(false);
        _orderRepository.GetByKeetaOrderIdAsync("order-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);
        var polledEvent = new KeetaPolledEvent("evt-1", "CONFIRMED", "order-1", "https://x", DateTime.UtcNow, "{}");

        var result = await _processor.ProcessAsync(1, 2, polledEvent, CancellationToken.None);

        result.Should().BeFalse();
        await _eventLogRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationOrderEventLog>(l => !l.ProcessedSuccessfully && l.ErrorMessage!.Contains("fora de ordem")),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("CONFIRMED", "CONFIRMED")]
    [InlineData("READY_FOR_PICKUP", "READY_FOR_PICKUP")]
    [InlineData("DELIVERED", "CONCLUDED")]
    [InlineData("CONCLUDED", "CONCLUDED")]
    [InlineData("DISPATCHED", "DISPATCHED")]
    public async Task ProcessAsync_OrderFound_ShouldApplyExpectedStatusTransition(string eventType, string expectedStatus)
    {
        var order = MakeOrder();
        _eventLogRepository.ExistsByEventIdAsync("evt-1", Arg.Any<CancellationToken>()).Returns(false);
        _orderRepository.GetByKeetaOrderIdAsync("order-1", Arg.Any<CancellationToken>()).Returns(order);
        var polledEvent = new KeetaPolledEvent("evt-1", eventType, "order-1", "https://x", DateTime.UtcNow, "{}");

        var result = await _processor.ProcessAsync(1, 2, polledEvent, CancellationToken.None);

        result.Should().BeTrue();
        order.Status.Should().Be(expectedStatus);
        _orderRepository.Received(1).Update(order);
        await _eventLogRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationOrderEventLog>(l => l.ProcessedSuccessfully), Arg.Any<CancellationToken>());
    }
}
