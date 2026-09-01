using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.RemoveItemComplement;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.RemoveItemComplement;

public sealed class RemoveOrderItemComplementCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RemoveOrderItemComplementCommandHandler _handler;

    public RemoveOrderItemComplementCommandHandlerTests()
    {
        _handler = new RemoveOrderItemComplementCommandHandler(
            _orderRepository, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOrderWithComplement()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;
        order.AddItem(productId: 1, unitPrice: 20m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var itemId = order.Items.First().Id;
        order.AddComplement(itemId, complementId: 0, unitPriceCharged: 5m, DateTime.Now);
        return order;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new RemoveOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: 0, OrderItemComplementId: 0, EmployeeId: 7);
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
        var order = CreateOrderWithComplement();
        var command = new RemoveOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: 999, OrderItemComplementId: 0, EmployeeId: 7);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.ItemNotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComplementNotFound_ShouldReturnFailure()
    {
        var order = CreateOrderWithComplement();
        var itemId = order.Items.First().Id;
        var command = new RemoveOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: itemId, OrderItemComplementId: 999, EmployeeId: 7);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.ComplementNotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingComplement_ShouldRemoveComplementAndCommitTwice()
    {
        var order = CreateOrderWithComplement();
        var item = order.Items.First();
        var complementId = item.Complements.First().Id;
        var command = new RemoveOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, OrderItemComplementId: complementId, EmployeeId: 7);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.Complements.Should().NotContain(c => c.Id == complementId && c.IsActive);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingComplement_ShouldRecordEmployeeIdInAuditLog()
    {
        var order = CreateOrderWithComplement();
        var item = order.Items.First();
        var complementId = item.Complements.First().Id;
        var command = new RemoveOrderItemComplementCommand(CustomerOrderId: 1, OrderItemId: item.Id, OrderItemComplementId: complementId, EmployeeId: 7);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        await _handler.Handle(command, CancellationToken.None);

        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.AppUserId == 7), Arg.Any<CancellationToken>());
    }
}
