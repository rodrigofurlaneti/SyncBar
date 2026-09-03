using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.TransferItem;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.TransferItem;

public sealed class TransferTableItemCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ITableItemTransferRepository _transferRepository = Substitute.For<ITableItemTransferRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly TransferTableItemCommandHandler _handler;

    public TransferTableItemCommandHandlerTests()
    {
        _handler = new TransferTableItemCommandHandler(
            _orderRepository, _transferRepository, _diningTableRepository,
            TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static void SetItemId(OrderItem item, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, id);

    private static CustomerOrder CreateTableOrder(long diningTableId, decimal? creditLimit = null)
        => CustomerOrder.Create(1, diningTableId, null, 1, null, null, DateTime.Now, creditLimit).Value;

    private static CustomerOrder CreateTableOrderWithItem(
        long diningTableId, long itemId, decimal unitPrice = 50m, decimal quantity = 1m,
        long orderItemStatusId = OrderItemStatusIds.Lancado)
    {
        var order = CreateTableOrder(diningTableId);
        order.AddItem(productId: 99, unitPrice: unitPrice, quantity: quantity, notes: "obs", employeeId: 5, DateTime.Now);
        var item = order.Items.Single();
        SetItemId(item, itemId);
        if (orderItemStatusId != OrderItemStatusIds.Lancado)
            order.UpdateItemStatus(itemId, orderItemStatusId, DateTime.Now);
        return order;
    }

    [Fact]
    public async Task Handle_SourceOrderNotFound_ReturnsFailure()
    {
        var command = new TransferTableItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.SourceNotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SourceOrderInactive_ReturnsFailure()
    {
        var sourceOrder = CreateTableOrderWithItem(100, 10);
        sourceOrder.Deactivate(DateTime.Now);
        var command = new TransferTableItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.SourceNotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFoundInSourceOrder_ReturnsFailure()
    {
        var sourceOrder = CreateTableOrderWithItem(100, 10);
        var command = new TransferTableItemCommand(1, 2, 999, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrderItem.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemAlreadyCancelled_ReturnsFailure()
    {
        var sourceOrder = CreateTableOrderWithItem(100, 10, orderItemStatusId: OrderItemStatusIds.Cancelado);
        var command = new TransferTableItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.AlreadyCancelled");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TargetOrderNotFound_ReturnsFailure()
    {
        var sourceOrder = CreateTableOrderWithItem(100, 10);
        var command = new TransferTableItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.TargetNotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TargetOrderInactive_ReturnsFailure()
    {
        var sourceOrder = CreateTableOrderWithItem(100, 10);
        var targetOrder = CreateTableOrder(200);
        targetOrder.Deactivate(DateTime.Now);
        var command = new TransferTableItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.TargetNotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameSourceAndTargetTable_ReturnsFailureFromTransferCreate()
    {
        var sourceOrder = CreateTableOrderWithItem(100, 10);
        var targetOrder = CreateTableOrder(200);
        var command = new TransferTableItemCommand(1, 2, 10, 100, 100, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableItemTransfer.SameTable");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_TransfersItemPreservingPriceQuantityStatus_FreesSourceTable()
    {
        var sourceOrder = CreateTableOrderWithItem(
            100, 10, unitPrice: 55.5m, quantity: 2m, orderItemStatusId: OrderItemStatusIds.Pronto);
        var targetOrder = CreateTableOrder(200);
        var sourceTable = DiningTable.Create(1, TableStatusIds.Ocupada, 3, null).Value;
        var command = new TransferTableItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);
        _diningTableRepository.GetByIdForUpdateAsync(command.SourceDiningTableId, Arg.Any<CancellationToken>())
            .Returns(sourceTable);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        sourceOrder.Items.Should().ContainSingle();
        sourceOrder.Items.Single().OrderItemStatusId.Should().Be(OrderItemStatusIds.Cancelado);

        targetOrder.Items.Should().ContainSingle();
        var transferredItem = targetOrder.Items.Single();
        transferredItem.UnitPrice.Should().Be(55.5m);
        transferredItem.Quantity.Should().Be(2m);
        transferredItem.Notes.Should().Be("obs");
        transferredItem.OrderItemStatusId.Should().Be(OrderItemStatusIds.Pronto);

        sourceTable.TableStatusId.Should().Be(TableStatusIds.Livre);
        sourceOrder.OrderStatusId.Should().Be(OrderStatusIds.Cancelado);

        await _transferRepository.Received(1).AddAsync(Arg.Any<TableItemTransfer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_SourceStillHasOtherActiveItems_KeepsTableInUse()
    {
        var sourceOrder = CreateTableOrderWithItem(100, 10, unitPrice: 30m);
        sourceOrder.AddItem(productId: 88, unitPrice: 20m, quantity: 1, notes: null, employeeId: 5, DateTime.Now);
        var targetOrder = CreateTableOrder(200);
        var sourceTable = DiningTable.Create(1, TableStatusIds.Ocupada, 3, null).Value;
        var command = new TransferTableItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);
        _diningTableRepository.GetByIdForUpdateAsync(command.SourceDiningTableId, Arg.Any<CancellationToken>())
            .Returns(sourceTable);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sourceTable.TableStatusId.Should().Be(TableStatusIds.Ocupada);
        sourceOrder.OrderStatusId.Should().NotBe(OrderStatusIds.Cancelado);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SourceTableNotFound_StillSucceeds()
    {
        var sourceOrder = CreateTableOrderWithItem(100, 10);
        var targetOrder = CreateTableOrder(200);
        var command = new TransferTableItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);
        _diningTableRepository.GetByIdForUpdateAsync(command.SourceDiningTableId, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
