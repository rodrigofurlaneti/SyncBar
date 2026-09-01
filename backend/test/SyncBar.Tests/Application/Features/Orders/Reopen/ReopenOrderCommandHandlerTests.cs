using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.Reopen;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.Reopen;

public sealed class ReopenOrderCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ReopenOrderCommandHandler _handler;

    public ReopenOrderCommandHandlerTests()
    {
        _handler = new ReopenOrderCommandHandler(
            _orderRepository, _diningTableRepository, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateTableOrder()
        => CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;

    private static CustomerOrder CreateComandaOrder()
        => CustomerOrder.Create(1, null, 20, 1, null, null, DateTime.Now).Value;

    private static CustomerOrder CreateAwaitingPaymentOrder(CustomerOrder order)
    {
        order.AddItem(productId: 1, unitPrice: 40m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        order.Close(serviceFeeRate: 0.10m, DateTime.Now);
        return order;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new ReopenOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderStillOpen_ShouldReturnFailure()
    {
        var order = CreateTableOrder();
        var command = new ReopenOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotAwaitingPayment");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderAlreadyPaid_ShouldReturnFailure()
    {
        var order = CreateAwaitingPaymentOrder(CreateTableOrder());
        order.MarkAsPaid(DateTime.Now);
        var command = new ReopenOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotAwaitingPayment");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TableOrder_ShouldReopenAndSetTableOccupied()
    {
        var order = CreateAwaitingPaymentOrder(CreateTableOrder());
        var table = DiningTable.Create(branchId: 1, tableStatusId: TableStatusIds.EmFechamento, number: 2, capacity: 4).Value;
        var command = new ReopenOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId!.Value, Arg.Any<CancellationToken>())
            .Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.EmAndamento);
        order.ServiceFeeAmount.Should().Be(0m);
        table.TableStatusId.Should().Be(TableStatusIds.Ocupada);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComandaOrder_ShouldReopenWithoutTouchingDiningTableRepository()
    {
        var order = CreateAwaitingPaymentOrder(CreateComandaOrder());
        var command = new ReopenOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.EmAndamento);
        await _diningTableRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
