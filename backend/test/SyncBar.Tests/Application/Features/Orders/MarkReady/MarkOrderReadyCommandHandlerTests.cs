using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.MarkReady;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.MarkReady;

public sealed class MarkOrderReadyCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orders = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logs = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private Task<SyncBar.Domain.Primitives.Result> Run(CustomerOrder? order)
    {
        _orders.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        return new MarkOrderReadyCommandHandler(_orders, TimeProvider.System, _logs, _unitOfWork)
            .Handle(new MarkOrderReadyCommand(1), CancellationToken.None);
    }

    private static CustomerOrder Delivery() => CustomerOrder.Create(1, null, null, 1, null, null,
        DateTime.Now, orderTypeId: OrderTypeIds.Delivery, customerName: "Cliente", deliveryAddress: "Rua 1").Value;

    [Fact]
    public async Task Ready_UpdatesAllItemsAndOrderWithoutChangingAmounts()
    {
        var order = Delivery();
        order.AddItem(1, 12m, 1, null, null, DateTime.Now);
        order.AddItem(2, 8m, 1, null, null, DateTime.Now);
        var amount = order.TotalAmount;
        var result = await Run(order);
        result.IsSuccess.Should().BeTrue();
        order.Items.Should().OnlyContain(i => i.OrderItemStatusId == OrderItemStatusIds.Pronto);
        order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
        order.TotalAmount.Should().Be(amount);
        order.ServiceFeeAmount.Should().Be(0);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ready_AdvancesImportedOrderWithoutMappedItems()
    {
        var order = Delivery();
        order.StartPreparation(DateTime.Now);
        (await Run(order)).IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
    }

    [Fact]
    public async Task Ready_DoesNotChangeCancelledOrder()
    {
        var order = Delivery();
        order.Cancel(DateTime.Now);
        (await Run(order)).IsFailure.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.Cancelado);
    }

    [Fact]
    public async Task Ready_RejectsTableOrder()
    {
        var order = CustomerOrder.Create(1, 1, null, 1, null, null, DateTime.Now).Value;
        order.StartPreparation(DateTime.Now);
        (await Run(order)).Error.Code.Should().Be("CustomerOrder.NotDelivery");
    }

    [Fact]
    public async Task Ready_RejectsNewOrderAndMissingOrder()
    {
        (await Run(Delivery())).Error.Code.Should().Be("CustomerOrder.NotInPreparation");
        (await Run(null)).Error.Code.Should().Be("CustomerOrder.NotFound");
    }
}
