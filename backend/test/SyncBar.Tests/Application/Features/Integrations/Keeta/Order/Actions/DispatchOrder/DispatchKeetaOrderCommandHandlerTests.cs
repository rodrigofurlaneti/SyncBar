using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.DispatchOrder;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.DispatchOrder;

public sealed class DispatchKeetaOrderCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly IKeetaOrderClient _orderClient = Substitute.For<IKeetaOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DispatchKeetaOrderCommandHandler _handler;

    public DispatchKeetaOrderCommandHandlerTests()
    {
        _handler = new DispatchKeetaOrderCommandHandler(_orderRepository, _orderClient, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrder MakeOrder() =>
        KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var command = new DispatchKeetaOrderCommand(1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_NoTrackingEventType_ShouldPassNullTrackingEvent()
    {
        var order = MakeOrder();
        var command = new DispatchKeetaOrderCommand(1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("DISPATCHED");
        await _orderClient.Received(1).DispatchOrderAsync(order.CompanyId, order.BranchId, order.KeetaOrderId, null, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TrackingEventTypeProvided_ShouldPassTrackingEvent()
    {
        var order = MakeOrder();
        var command = new DispatchKeetaOrderCommand(1, "DELIVERY_ONGOING", "saiu para entrega");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.Received(1).DispatchOrderAsync(
            order.CompanyId, order.BranchId, order.KeetaOrderId,
            Arg.Is<KeetaDeliveryTrackingEvent>(e => e.Type == "DELIVERY_ONGOING" && e.Message == "saiu para entrega"),
            Arg.Any<CancellationToken>());
    }
}
