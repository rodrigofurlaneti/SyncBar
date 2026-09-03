using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.UpdateItemStatus;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.UpdateItemStatus;

public sealed class UpdateOrderItemStatusCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IWaiterMessageRepository _messageRepository = Substitute.For<IWaiterMessageRepository>();
    private readonly IDiningAreaTableRepository _diningAreaTableRepository = Substitute.For<IDiningAreaTableRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IDiningAreaRepository _diningAreaRepository = Substitute.For<IDiningAreaRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateOrderItemStatusCommandHandler _handler;

    public UpdateOrderItemStatusCommandHandlerTests()
    {
        _handler = new UpdateOrderItemStatusCommandHandler(
            _orderRepository, _messageRepository, _diningAreaTableRepository, _diningTableRepository,
            _productRepository, _diningAreaRepository, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateTableOrder(long diningTableId = 10)
        => CustomerOrder.Create(1, diningTableId, null, 3, null, null, DateTime.Now).Value;

    private static CustomerOrder CreateComandaOrder()
        => CustomerOrder.Create(1, null, 20, 3, null, null, DateTime.Now).Value;

    private static DiningArea CreateDiningArea(long id, long branchId = 1)
    {
        var area = DiningArea.Create(branchId, "Salão").Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(area, id);
        return area;
    }

    private void SetupOrder(CustomerOrder order, long orderId = 1)
        => _orderRepository.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);
        var command = new UpdateOrderItemStatusCommand(CustomerOrderId: 1, OrderItemId: 1, OrderItemStatusId: OrderItemStatusIds.EnviadoCozinha);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ShouldReturnFailure()
    {
        var order = CreateTableOrder();
        SetupOrder(order);
        var command = new UpdateOrderItemStatusCommand(CustomerOrderId: 1, OrderItemId: 999, OrderItemStatusId: OrderItemStatusIds.EnviadoCozinha);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.ItemNotFound");
    }

    [Fact]
    public async Task Handle_CancelingItemAlreadySentToKitchenAsNonManager_ShouldReturnFailure()
    {
        var order = CreateTableOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.EnviadoCozinha, DateTime.Now); // já saiu de "Lançado"
        SetupOrder(order);
        var command = new UpdateOrderItemStatusCommand(
            CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.Cancelado, IsManager: false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.CancelRequiresManager");
        item.OrderItemStatusId.Should().Be(OrderItemStatusIds.EnviadoCozinha);
    }

    [Fact]
    public async Task Handle_CancelingItemAlreadySentToKitchenAsManager_ShouldSucceed()
    {
        var order = CreateTableOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.EnviadoCozinha, DateTime.Now);
        SetupOrder(order);
        var command = new UpdateOrderItemStatusCommand(
            CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.Cancelado, IsManager: true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.OrderItemStatusId.Should().Be(OrderItemStatusIds.Cancelado);
    }

    [Fact]
    public async Task Handle_CancelingItemStillLancadoAsNonManager_ShouldSucceed()
    {
        var order = CreateTableOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First(); // ainda Lançado
        SetupOrder(order);
        var command = new UpdateOrderItemStatusCommand(
            CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.Cancelado, IsManager: false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.OrderItemStatusId.Should().Be(OrderItemStatusIds.Cancelado);
    }

    [Fact]
    public async Task Handle_ItemAlreadyDelivered_ShouldReturnFinalStatusFailure()
    {
        var order = CreateTableOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.EnviadoCozinha, DateTime.Now);
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.EmPreparo, DateTime.Now);
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.Pronto, DateTime.Now);
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.Entregue, DateTime.Now);
        SetupOrder(order);
        var command = new UpdateOrderItemStatusCommand(
            CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.EnviadoCozinha, IsManager: true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.FinalStatus");
    }

    [Fact]
    public async Task Handle_StatusPronto_TableOrderWithoutRegisteredDiningArea_ShouldReturnFailure()
    {
        var order = CreateTableOrder(diningTableId: 10);
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        SetupOrder(order);
        _diningTableRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(DiningTable.Create(1, TableStatusIds.Ocupada, 5, 4).Value);
        _diningAreaTableRepository.GetByTableIdAsync(10, Arg.Any<CancellationToken>()).Returns((DiningAreaTable?)null);
        var command = new UpdateOrderItemStatusCommand(CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.Pronto);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WaiterMessage.DiningAreaRequired");
        // O item já teve o status alterado no agregado antes da notificação falhar — mas como o
        // commit não é chamado no caminho de falha, a mudança não é persistida.
        item.OrderItemStatusId.Should().Be(OrderItemStatusIds.Pronto);
    }

    [Fact]
    public async Task Handle_StatusPronto_TableOrder_ShouldSendWaiterMessageToTableDiningArea()
    {
        var order = CreateTableOrder(diningTableId: 10);
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        SetupOrder(order);
        var table = DiningTable.Create(1, TableStatusIds.Ocupada, 7, 4).Value;
        _diningTableRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(table);
        var areaTable = DiningAreaTable.Create(diningAreaId: 4, diningTableId: 10).Value;
        _diningAreaTableRepository.GetByTableIdAsync(10, Arg.Any<CancellationToken>()).Returns(areaTable);
        var product = Product.Create(1, 1, 1, "Hambúrguer", null, null, 10m, null, false, null).Value;
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);
        var command = new UpdateOrderItemStatusCommand(CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.Pronto, ActorEmployeeId: 9);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _messageRepository.Received(1).AddAsync(
            Arg.Is<WaiterMessage>(m => m.DiningAreaId == 4 && m.Message.Contains("Hambúrguer") && m.Message.Contains("Mesa 7") && m.SenderEmployeeId == 9),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StatusPronto_ComandaOrder_ShouldSendWaiterMessageToEachBranchDiningArea()
    {
        var order = CreateComandaOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        SetupOrder(order);
        var areaOne = CreateDiningArea(1);
        var areaTwo = CreateDiningArea(2);
        _diningAreaRepository.GetByBranchIdAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns(new List<DiningArea> { areaOne, areaTwo });
        var command = new UpdateOrderItemStatusCommand(CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.Pronto, ActorEmployeeId: 9);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _messageRepository.Received(2).AddAsync(Arg.Any<WaiterMessage>(), Arg.Any<CancellationToken>());
        await _messageRepository.Received(1).AddAsync(Arg.Is<WaiterMessage>(m => m.DiningAreaId == 1), Arg.Any<CancellationToken>());
        await _messageRepository.Received(1).AddAsync(Arg.Is<WaiterMessage>(m => m.DiningAreaId == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StatusPronto_ComandaOrderWithoutRegisteredAreas_ShouldDefaultToAreaOne()
    {
        var order = CreateComandaOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        SetupOrder(order);
        _diningAreaRepository.GetByBranchIdAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns(new List<DiningArea>());
        var command = new UpdateOrderItemStatusCommand(CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.Pronto, ActorEmployeeId: 9);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _messageRepository.Received(1).AddAsync(Arg.Is<WaiterMessage>(m => m.DiningAreaId == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConcurrencyExceptionOnCommit_ShouldPropagateExceptionInsteadOfTranslatingIt()
    {
        // Diferente dos outros 3 handlers deste módulo, UpdateOrderItemStatusCommandHandler NÃO
        // envolve o commit em try/catch para ConcurrencyException — a exceção se propaga.
        var order = CreateTableOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        SetupOrder(order);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new ConcurrencyException("Estoque alterado concorrentemente."));
        var command = new UpdateOrderItemStatusCommand(CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.EnviadoCozinha);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task Handle_ValidStatusUpdateWithoutNotification_ShouldUpdateStatusAndCommitTwice()
    {
        var order = CreateTableOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var item = order.Items.First();
        SetupOrder(order);
        var command = new UpdateOrderItemStatusCommand(CustomerOrderId: 1, OrderItemId: item.Id, OrderItemStatusId: OrderItemStatusIds.EnviadoCozinha);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.OrderItemStatusId.Should().Be(OrderItemStatusIds.EnviadoCozinha);
        item.SentToKitchenAt.Should().NotBeNull();
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<WaiterMessage>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
