using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.Close;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainServiceFeeSetting = SyncBar.Domain.Entities.ServiceFeeSetting;

namespace SyncBar.Tests.Application.Features.Orders.Close;

public sealed class CloseOrderCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IServiceFeeSettingRepository _serviceFeeSettingRepository = Substitute.For<IServiceFeeSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CloseOrderCommandHandler _handler;

    public CloseOrderCommandHandlerTests()
    {
        _handler = new CloseOrderCommandHandler(
            _orderRepository, _diningTableRepository, _serviceFeeSettingRepository,
            TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOpenOrderWithTable(long diningTableId = 10, decimal unitPrice = 100m)
    {
        var order = CustomerOrder.Create(1, diningTableId, null, 1, null, null, DateTime.Now).Value;
        order.AddItem(productId: 1, unitPrice: unitPrice, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        return order;
    }

    private static CustomerOrder CreateOpenOrderWithComanda(long comandaId = 20, decimal unitPrice = 100m)
    {
        var order = CustomerOrder.Create(1, null, comandaId, 1, null, null, DateTime.Now).Value;
        order.AddItem(productId: 1, unitPrice: unitPrice, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        return order;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        var command = new CloseOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderInactive_ReturnsFailure()
    {
        var order = CreateOpenOrderWithTable();
        order.Deactivate(DateTime.Now);
        var command = new CloseOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderWithNoActiveItems_ReturnsFailureFromDomainClose()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;
        var command = new CloseOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns((DomainServiceFeeSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NoItems");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceFeeSettingNull_DefaultsToEnabled_UsesRequestedRate()
    {
        var order = CreateOpenOrderWithTable(unitPrice: 100m);
        var command = new CloseOrderCommand(CustomerOrderId: 1, ServiceFeeRate: 0.10m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns((DomainServiceFeeSetting?)null);
        _diningTableRepository.GetByIdForUpdateAsync(10, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.ServiceFeeAmount.Should().Be(10m);
        order.TotalAmount.Should().Be(110m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceFeeSettingDisabled_UsesZeroRateEvenWhenRequested()
    {
        var order = CreateOpenOrderWithTable(unitPrice: 100m);
        var command = new CloseOrderCommand(CustomerOrderId: 1, ServiceFeeRate: 0.10m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns(DomainServiceFeeSetting.Create(1, enabled: false).Value);
        _diningTableRepository.GetByIdForUpdateAsync(10, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.ServiceFeeAmount.Should().Be(0m);
        order.TotalAmount.Should().Be(100m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceFeeSettingEnabled_UsesRequestedRate()
    {
        var order = CreateOpenOrderWithTable(unitPrice: 200m);
        var command = new CloseOrderCommand(CustomerOrderId: 1, ServiceFeeRate: 0.05m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns(DomainServiceFeeSetting.Create(1, enabled: true).Value);
        _diningTableRepository.GetByIdForUpdateAsync(10, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.ServiceFeeAmount.Should().Be(10m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderWithDiningTable_ChangesTableStatusToEmFechamento()
    {
        var order = CreateOpenOrderWithTable(diningTableId: 10, unitPrice: 50m);
        var table = DiningTable.Create(1, TableStatusIds.Ocupada, 5, null).Value;
        var command = new CloseOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns((DomainServiceFeeSetting?)null);
        _diningTableRepository.GetByIdForUpdateAsync(10, Arg.Any<CancellationToken>())
            .Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table.TableStatusId.Should().Be(TableStatusIds.EmFechamento);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderWithDiningTable_TableNotFound_StillSucceeds()
    {
        var order = CreateOpenOrderWithTable(diningTableId: 10, unitPrice: 50m);
        var command = new CloseOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns((DomainServiceFeeSetting?)null);
        _diningTableRepository.GetByIdForUpdateAsync(10, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderWithoutDiningTable_DoesNotQueryDiningTableRepository()
    {
        var order = CreateOpenOrderWithComanda(comandaId: 20, unitPrice: 50m);
        var command = new CloseOrderCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns((DomainServiceFeeSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _diningTableRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
