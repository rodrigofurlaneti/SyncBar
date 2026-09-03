using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.TransferTableItems;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.TransferTableItems;

public sealed class TransferTableItemsCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ITableItemTransferRepository _transferRepository = Substitute.For<ITableItemTransferRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly TransferTableItemsCommandHandler _handler;

    public TransferTableItemsCommandHandlerTests()
    {
        _handler = new TransferTableItemsCommandHandler(
            _orderRepository, _transferRepository, _diningTableRepository,
            TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static void SetItemId(OrderItem item, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, id);

    private static CustomerOrder CreateTableOrder(long diningTableId)
        => CustomerOrder.Create(1, diningTableId, null, 1, null, null, DateTime.Now).Value;

    private static OrderItem AddItemWithId(
        CustomerOrder order, long itemId, decimal unitPrice = 50m, decimal quantity = 1m,
        long orderItemStatusId = OrderItemStatusIds.Lancado)
    {
        order.AddItem(productId: 99, unitPrice: unitPrice, quantity: quantity, notes: $"item-{itemId}", employeeId: 5, DateTime.Now);
        var item = order.Items.Last();
        SetItemId(item, itemId);
        if (orderItemStatusId != OrderItemStatusIds.Lancado)
            order.UpdateItemStatus(itemId, orderItemStatusId, DateTime.Now);
        return item;
    }

    [Fact]
    public async Task Handle_SourceOrderNotFound_ReturnsFailure()
    {
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 10 }, 100, 200, 5);
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
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 10);
        sourceOrder.Deactivate(DateTime.Now);
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 10 }, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.SourceNotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TargetOrderNotFound_ReturnsFailure()
    {
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 10);
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 10 }, 100, 200, 5);
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
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 10);
        var targetOrder = CreateTableOrder(200);
        targetOrder.Deactivate(DateTime.Now);
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 10 }, 100, 200, 5);
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
    public async Task Handle_ItemNotFoundInSourceOrder_ReturnsFailureNamingTheMissingItem()
    {
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 10);
        var targetOrder = CreateTableOrder(200);
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 10, 999 }, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrderItem.NotFound");
        result.Error.Message.Should().Contain("999");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemAlreadyCancelled_ReturnsFailure()
    {
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 10, orderItemStatusId: OrderItemStatusIds.Cancelado);
        var targetOrder = CreateTableOrder(200);
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 10 }, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.AlreadyCancelled");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameSourceAndTargetTable_ReturnsFailureFromTransferCreate()
    {
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 10);
        var targetOrder = CreateTableOrder(200);
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 10 }, 100, 100, 5);
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
    public async Task Handle_HappyPath_TransfersMultipleItemsInSameBatch_NoneStuckInSource()
    {
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 11, unitPrice: 20m, quantity: 1m, orderItemStatusId: OrderItemStatusIds.Lancado);
        AddItemWithId(sourceOrder, 12, unitPrice: 35m, quantity: 3m, orderItemStatusId: OrderItemStatusIds.Entregue);
        var targetOrder = CreateTableOrder(200);
        var sourceTable = DiningTable.Create(1, TableStatusIds.Ocupada, 3, null).Value;
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 12, 11 }, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);
        _diningTableRepository.GetByIdForUpdateAsync(command.SourceDiningTableId, Arg.Any<CancellationToken>())
            .Returns(sourceTable);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        sourceOrder.Items.Should().HaveCount(2);
        sourceOrder.Items.Should().OnlyContain(i => i.OrderItemStatusId == OrderItemStatusIds.Cancelado);

        targetOrder.Items.Should().HaveCount(2);
        targetOrder.Items.Should().Contain(i =>
            i.UnitPrice == 20m && i.Quantity == 1m && i.OrderItemStatusId == OrderItemStatusIds.Lancado);
        targetOrder.Items.Should().Contain(i =>
            i.UnitPrice == 35m && i.Quantity == 3m && i.OrderItemStatusId == OrderItemStatusIds.Entregue);

        sourceTable.TableStatusId.Should().Be(TableStatusIds.Livre);
        sourceOrder.OrderStatusId.Should().Be(OrderStatusIds.Cancelado);

        await _transferRepository.Received(2).AddAsync(Arg.Any<TableItemTransfer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_PartialTransfer_SourceKeepsRemainingItemAndStaysInUse()
    {
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 11, unitPrice: 20m);
        AddItemWithId(sourceOrder, 12, unitPrice: 35m);
        var targetOrder = CreateTableOrder(200);
        var sourceTable = DiningTable.Create(1, TableStatusIds.Ocupada, 3, null).Value;
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 11 }, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);
        _diningTableRepository.GetByIdForUpdateAsync(command.SourceDiningTableId, Arg.Any<CancellationToken>())
            .Returns(sourceTable);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetOrder.Items.Should().ContainSingle();
        sourceOrder.Items.Should().Contain(i => i.OrderItemStatusId != OrderItemStatusIds.Cancelado);
        sourceTable.TableStatusId.Should().Be(TableStatusIds.Ocupada);
        sourceOrder.OrderStatusId.Should().NotBe(OrderStatusIds.Cancelado);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SourceTableNotFound_StillSucceeds()
    {
        var sourceOrder = CreateTableOrder(100);
        AddItemWithId(sourceOrder, 10);
        var targetOrder = CreateTableOrder(200);
        var command = new TransferTableItemsCommand(1, 2, new List<long> { 10 }, 100, 200, 5);
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
