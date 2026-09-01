using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.RemoveServiceFee;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.RemoveServiceFee;

public sealed class RemoveServiceFeeCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RemoveServiceFeeCommandHandler _handler;

    public RemoveServiceFeeCommandHandlerTests()
    {
        _handler = new RemoveServiceFeeCommandHandler(
            _orderRepository, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOpenOrder()
        => CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new RemoveServiceFeeCommand(CustomerOrderId: 1);
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
        var order = CreateOpenOrder();
        var command = new RemoveServiceFeeCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotAwaitingPayment");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AwaitingPaymentWithoutServiceFee_ShouldReturnFailure()
    {
        var order = CreateOpenOrder();
        order.AddItem(productId: 1, unitPrice: 30m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        order.Close(serviceFeeRate: 0m, DateTime.Now);
        var command = new RemoveServiceFeeCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NoServiceFee");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AwaitingPaymentWithServiceFee_ShouldRemoveFeeAndCommitTwice()
    {
        var order = CreateOpenOrder();
        order.AddItem(productId: 1, unitPrice: 100m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        order.Close(serviceFeeRate: 0.10m, DateTime.Now);
        order.ServiceFeeAmount.Should().Be(10m);
        var command = new RemoveServiceFeeCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.ServiceFeeAmount.Should().Be(0m);
        order.TotalAmount.Should().Be(100m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
