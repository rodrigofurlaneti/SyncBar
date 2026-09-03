using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.TransferComandaItem;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.TransferComandaItem;

public sealed class TransferComandaItemCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IComandaItemTransferRepository _transferRepository = Substitute.For<IComandaItemTransferRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly TransferComandaItemCommandHandler _handler;

    public TransferComandaItemCommandHandlerTests()
    {
        _handler = new TransferComandaItemCommandHandler(
            _orderRepository, _transferRepository, _comandaRepository,
            TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static void SetItemId(OrderItem item, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, id);

    private static CustomerOrder CreateComandaOrder(long comandaId, decimal? creditLimit = null)
        => CustomerOrder.Create(1, null, comandaId, 1, null, null, DateTime.Now, creditLimit).Value;

    private static CustomerOrder CreateComandaOrderWithItem(
        long comandaId, long itemId, decimal unitPrice = 50m, decimal quantity = 1m,
        long orderItemStatusId = OrderItemStatusIds.Lancado, decimal? creditLimit = null)
    {
        var order = CreateComandaOrder(comandaId, creditLimit);
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
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
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
        var sourceOrder = CreateComandaOrderWithItem(100, 10);
        sourceOrder.Deactivate(DateTime.Now);
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
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
        var sourceOrder = CreateComandaOrderWithItem(100, 10);
        var command = new TransferComandaItemCommand(1, 2, 999, 100, 200, 5);
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
        var sourceOrder = CreateComandaOrderWithItem(100, 10, orderItemStatusId: OrderItemStatusIds.Cancelado);
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
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
        var sourceOrder = CreateComandaOrderWithItem(100, 10);
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
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
        var sourceOrder = CreateComandaOrderWithItem(100, 10);
        var targetOrder = CreateComandaOrder(200);
        targetOrder.Deactivate(DateTime.Now);
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
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
    public async Task Handle_SameSourceAndTargetComanda_ReturnsFailureFromTransferCreate()
    {
        var sourceOrder = CreateComandaOrderWithItem(100, 10);
        var targetOrder = CreateComandaOrder(200);
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 100, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComandaItemTransfer.SameComanda");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddTransferredItemExceedsCreditLimit_ReturnsFailure()
    {
        var sourceOrder = CreateComandaOrderWithItem(100, 10, unitPrice: 100m);
        var targetOrder = CreateComandaOrder(200, creditLimit: 10m);
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.LimitExceeded");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_TransfersItemPreservingPriceQuantityStatus_EmptiesSourceComanda()
    {
        var sourceOrder = CreateComandaOrderWithItem(
            100, 10, unitPrice: 55.5m, quantity: 2m, orderItemStatusId: OrderItemStatusIds.Pronto);
        var targetOrder = CreateComandaOrder(200);
        var sourceComanda = Comanda.Create(1, ComandaStatusIds.EmUso, "C-100").Value;
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);
        _comandaRepository.GetByIdForUpdateAsync(command.SourceComandaId, Arg.Any<CancellationToken>())
            .Returns(sourceComanda);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Não deve haver mais item "preso" ativo na origem — o único item vira Cancelado.
        sourceOrder.Items.Should().ContainSingle();
        sourceOrder.Items.Single().OrderItemStatusId.Should().Be(OrderItemStatusIds.Cancelado);

        // O item deve aparecer no destino com o MESMO preço, quantidade e status original.
        targetOrder.Items.Should().ContainSingle();
        var transferredItem = targetOrder.Items.Single();
        transferredItem.UnitPrice.Should().Be(55.5m);
        transferredItem.Quantity.Should().Be(2m);
        transferredItem.Notes.Should().Be("obs");
        transferredItem.OrderItemStatusId.Should().Be(OrderItemStatusIds.Pronto);

        sourceComanda.ComandaStatusId.Should().Be(ComandaStatusIds.Disponivel);
        sourceOrder.OrderStatusId.Should().Be(OrderStatusIds.Cancelado);

        await _transferRepository.Received(1).AddAsync(Arg.Any<ComandaItemTransfer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_SourceStillHasOtherActiveItems_KeepsComandaInUse()
    {
        var sourceOrder = CreateComandaOrderWithItem(100, 10, unitPrice: 30m);
        sourceOrder.AddItem(productId: 88, unitPrice: 20m, quantity: 1, notes: null, employeeId: 5, DateTime.Now);
        var targetOrder = CreateComandaOrder(200);
        var sourceComanda = Comanda.Create(1, ComandaStatusIds.EmUso, "C-100").Value;
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);
        _comandaRepository.GetByIdForUpdateAsync(command.SourceComandaId, Arg.Any<CancellationToken>())
            .Returns(sourceComanda);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sourceComanda.ComandaStatusId.Should().Be(ComandaStatusIds.EmUso);
        sourceOrder.OrderStatusId.Should().NotBe(OrderStatusIds.Cancelado);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SourceComandaNotFound_StillSucceeds()
    {
        var sourceOrder = CreateComandaOrderWithItem(100, 10);
        var targetOrder = CreateComandaOrder(200);
        var command = new TransferComandaItemCommand(1, 2, 10, 100, 200, 5);
        _orderRepository.GetByIdForUpdateAsync(command.SourceCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(sourceOrder);
        _orderRepository.GetByIdForUpdateAsync(command.TargetCustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(targetOrder);
        _comandaRepository.GetByIdForUpdateAsync(command.SourceComandaId, Arg.Any<CancellationToken>())
            .Returns((Comanda?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
