using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.AddItemComplement;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.AddItemComplement;

public sealed class AddOrderItemComplementCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly IProductStockRepository _stockRepository = Substitute.For<IProductStockRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddOrderItemComplementCommandHandler _handler;

    public AddOrderItemComplementCommandHandlerTests()
    {
        _handler = new AddOrderItemComplementCommandHandler(
            _orderRepository, _complementGroupRepository, _productComplementGroupRepository,
            _complementItemRepository, _stockRepository, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOrderWithItem(long productId = 5, decimal quantity = 2m)
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;
        order.AddItem(productId, unitPrice: 20m, quantity, notes: null, employeeId: null, DateTime.Now);
        return order;
    }

    private static ComplementItem CreateComplementItem(long id, long? linkedProductId = null)
    {
        var item = ComplementItem.Create(1, "Bacon extra", linkedProductId).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, id);
        return item;
    }

    private void SetupOrder(CustomerOrder order, long orderId = 1)
        => _orderRepository.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: 0, ComplementGroupId: 5, ComplementId: 1, EmployeeId: 7);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ShouldReturnFailure()
    {
        var order = CreateOrderWithItem();
        SetupOrder(order);
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: 999, ComplementGroupId: 5, ComplementId: 1, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.ItemNotFound");
    }

    [Fact]
    public async Task Handle_ItemAlreadyDelivered_ShouldReturnFinalStatusFailure()
    {
        var order = CreateOrderWithItem();
        var item = order.Items.First();
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.EnviadoCozinha, DateTime.Now);
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.EmPreparo, DateTime.Now);
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.Pronto, DateTime.Now);
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.Entregue, DateTime.Now);
        SetupOrder(order);
        var link = ProductComplementGroup.Create(item.ProductId, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(item.ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Adicionais", ComplementGroupTypeIds.Ingredientes, 0, 5).Value;
        var complement = group.AddComplement(complementItemId: 1, extraPrice: 5m).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, ComplementGroupId: 5, ComplementId: complement.Id, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.FinalStatus");
    }

    [Fact]
    public async Task Handle_ComplementGroupNotLinkedToItemProduct_ShouldReturnFailure()
    {
        var order = CreateOrderWithItem();
        var item = order.Items.First();
        SetupOrder(order);
        _productComplementGroupRepository.GetByProductAsync(item.ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup>());
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, ComplementGroupId: 5, ComplementId: 1, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.ComplementGroupNotAvailable");
    }

    [Fact]
    public async Task Handle_ComplementGroupNotFoundOrInactive_ShouldReturnFailure()
    {
        var order = CreateOrderWithItem();
        var item = order.Items.First();
        SetupOrder(order);
        var link = ProductComplementGroup.Create(item.ProductId, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(item.ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((ComplementGroup?)null);
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, ComplementGroupId: 5, ComplementId: 1, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_ComplementNotFoundInGroup_ShouldReturnFailure()
    {
        var order = CreateOrderWithItem();
        var item = order.Items.First();
        SetupOrder(order);
        var link = ProductComplementGroup.Create(item.ProductId, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(item.ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Adicionais", ComplementGroupTypeIds.Ingredientes, 0, 5).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, ComplementGroupId: 5, ComplementId: 999, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
    }

    [Fact]
    public async Task Handle_LinkedProductStockInsufficient_ShouldReturnFailure()
    {
        var order = CreateOrderWithItem(quantity: 3m);
        var item = order.Items.First();
        SetupOrder(order);
        var link = ProductComplementGroup.Create(item.ProductId, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(item.ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Combo", ComplementGroupTypeIds.Especificacao, 0, 5).Value;
        var complement = group.AddComplement(complementItemId: 30, extraPrice: 0m).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        var complementItem = CreateComplementItem(30, linkedProductId: 999);
        _complementItemRepository.GetByIdAsync(30, Arg.Any<CancellationToken>()).Returns(complementItem);
        _stockRepository.GetByProductIdAsync(999, Arg.Any<CancellationToken>())
            .Returns(new ProductStock(999, initialBalance: 1m, minimumQuantity: 0m)); // insuficiente p/ Quantity=3
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, ComplementGroupId: 5, ComplementId: complement.Id, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stock.Insufficient");
    }

    [Fact]
    public async Task Handle_ConcurrencyExceptionOnCommit_ShouldReturnFailureAndCommitTwice()
    {
        var order = CreateOrderWithItem();
        var item = order.Items.First();
        SetupOrder(order);
        var link = ProductComplementGroup.Create(item.ProductId, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(item.ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Adicionais", ComplementGroupTypeIds.Ingredientes, 0, 5).Value;
        var complement = group.AddComplement(complementItemId: 1, extraPrice: 5m).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new ConcurrencyException("Estoque alterado concorrentemente."));
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, ComplementGroupId: 5, ComplementId: complement.Id, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stock.Concurrency");
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithoutLinkedProduct_ShouldAddComplementAndCommitTwice()
    {
        var order = CreateOrderWithItem();
        var item = order.Items.First();
        SetupOrder(order);
        var link = ProductComplementGroup.Create(item.ProductId, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(item.ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Adicionais", ComplementGroupTypeIds.Ingredientes, 0, 5).Value;
        var complement = group.AddComplement(complementItemId: 1, extraPrice: 6m).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, ComplementGroupId: 5, ComplementId: complement.Id, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.Complements.Should().ContainSingle(c => c.ComplementId == complement.Id && c.UnitPriceCharged == 6m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithLinkedProduct_ShouldDeductLinkedStockByItemQuantity()
    {
        var order = CreateOrderWithItem(quantity: 3m);
        var item = order.Items.First();
        SetupOrder(order);
        var link = ProductComplementGroup.Create(item.ProductId, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(item.ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Combo", ComplementGroupTypeIds.Especificacao, 0, 5).Value;
        var complement = group.AddComplement(complementItemId: 30, extraPrice: 0m).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        var complementItem = CreateComplementItem(30, linkedProductId: 999);
        _complementItemRepository.GetByIdAsync(30, Arg.Any<CancellationToken>()).Returns(complementItem);
        var linkedStock = new ProductStock(999, initialBalance: 10m, minimumQuantity: 0m);
        _stockRepository.GetByProductIdAsync(999, Arg.Any<CancellationToken>()).Returns(linkedStock);
        var command = new AddOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, ComplementGroupId: 5, ComplementId: complement.Id, EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        linkedStock.CurrentBalance.Should().Be(7m); // 10 - Quantity(3) da linha do item
        _stockRepository.Received(1).AddMovement(Arg.Is<StockMovement>(m => m.Quantity == -3m && m.OrderItemId == item.Id));
    }
}
