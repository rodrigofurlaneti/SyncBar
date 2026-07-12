using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.UpdateItemStatus;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class CancelAuditTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateOrderItemStatusCommandHandler CreateHandler()
        => new(_orderRepository, _unitOfWork);

    private static CustomerOrder OrderWithSentItem()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(1, 30m, 1, null, null);
        var item = order.Items.First();
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.EnviadoCozinha);
        return order;
    }

    [Fact]
    public async Task CancelSentItem_AsWaiter_ShouldBeBlocked()
    {
        var order = OrderWithSentItem();
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await CreateHandler().Handle(new UpdateOrderItemStatusCommand(
            1, order.Items.First().Id, OrderItemStatusIds.Cancelado, ActorEmployeeId: 3, IsManager: false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.CancelRequiresManager");
    }

    [Fact]
    public async Task CancelSentItem_AsManager_ShouldSucceedAndRecordWho()
    {
        var order = OrderWithSentItem();
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await CreateHandler().Handle(new UpdateOrderItemStatusCommand(
            1, order.Items.First().Id, OrderItemStatusIds.Cancelado, ActorEmployeeId: 9, IsManager: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.First().OrderItemStatusId.Should().Be(OrderItemStatusIds.Cancelado);
        order.Items.First().CancelledByEmployeeId.Should().Be(9);
    }

    [Fact]
    public async Task CancelFreshItem_AsWaiter_ShouldSucceed()
    {
        // Item ainda Lancado (nao foi para a cozinha) — garcom corrige na hora.
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(1, 30m, 1, null, null);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await CreateHandler().Handle(new UpdateOrderItemStatusCommand(
            1, order.Items.First().Id, OrderItemStatusIds.Cancelado, ActorEmployeeId: 3, IsManager: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
