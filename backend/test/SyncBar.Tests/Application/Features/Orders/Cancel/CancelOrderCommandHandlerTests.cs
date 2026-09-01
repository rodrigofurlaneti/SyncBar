using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.Cancel;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.Cancel;

public sealed class CancelOrderCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _handler = new CancelOrderCommandHandler(
            _orderRepository, _diningTableRepository, _comandaRepository,
            TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateTableOrder()
        => CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;

    private static CustomerOrder CreateComandaOrder()
        => CustomerOrder.Create(1, null, 20, 1, null, null, DateTime.Now).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new CancelOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderAlreadyPaid_ShouldReturnFailure()
    {
        var order = CreateTableOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        order.Close(serviceFeeRate: 0m, DateTime.Now);
        order.MarkAsPaid(DateTime.Now);
        var command = new CancelOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.AlreadyPaid");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderAlreadyCancelled_ShouldReturnFailure()
    {
        var order = CreateTableOrder();
        order.Cancel(DateTime.Now);
        var command = new CancelOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.AlreadyCancelled");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TableOrder_ShouldCancelAndFreeTable()
    {
        var order = CreateTableOrder();
        var table = DiningTable.Create(branchId: 1, tableStatusId: TableStatusIds.Ocupada, number: 5, capacity: 4).Value;
        var command = new CancelOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId!.Value, Arg.Any<CancellationToken>())
            .Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.Cancelado);
        table.TableStatusId.Should().Be(TableStatusIds.Livre);
        await _comandaRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComandaOrder_ShouldCancelAndFreeComanda()
    {
        var order = CreateComandaOrder();
        var comanda = Comanda.Create(branchId: 1, comandaStatusId: ComandaStatusIds.EmUso, code: "C01").Value;
        var command = new CancelOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _comandaRepository.GetByIdForUpdateAsync(order.ComandaId!.Value, Arg.Any<CancellationToken>())
            .Returns(comanda);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.Cancelado);
        comanda.ComandaStatusId.Should().Be(ComandaStatusIds.Disponivel);
        await _diningTableRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
