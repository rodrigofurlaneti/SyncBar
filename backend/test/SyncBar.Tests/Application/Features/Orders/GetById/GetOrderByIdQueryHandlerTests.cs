using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.GetById;

public sealed class GetOrderByIdQueryHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository = Substitute.For<IOrderPartialPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _handler = new GetOrderByIdQueryHandler(_orderRepository, _partialPaymentRepository, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOpenOrder(long branchId = 1, long diningTableId = 1, long employeeId = 1)
        => CustomerOrder.Create(branchId, diningTableId, null, employeeId, 4, null, DateTime.Now).Value;

    private static OrderPartialPayment CreatePartialPayment(decimal amount)
        => OrderPartialPayment.Create(1, 1, 1, 1, amount, null, null).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnCustomerOrderNotFound()
    {
        var query = new GetOrderByIdQuery(CustomerOrderId: 99);
        _orderRepository.GetByIdAsync(query.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _partialPaymentRepository.DidNotReceive().GetByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderInactive_ShouldReturnCustomerOrderNotFound()
    {
        var order = CreateOpenOrder();
        order.Deactivate(DateTime.Now);
        var query = new GetOrderByIdQuery(CustomerOrderId: order.Id);
        _orderRepository.GetByIdAsync(query.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderFoundNoPartialPayments_ShouldReturnResponseWithZeroPartialPaidAmount()
    {
        var order = CreateOpenOrder();
        var query = new GetOrderByIdQuery(CustomerOrderId: order.Id);
        _orderRepository.GetByIdAsync(query.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _partialPaymentRepository.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(order.Id);
        result.Value.BranchId.Should().Be(order.BranchId);
        result.Value.TotalAmount.Should().Be(order.TotalAmount);
        result.Value.PartialPaidAmount.Should().Be(0);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderFoundWithTwoPartialPayments_ShouldReturnResponseWithSummedPartialPaidAmount()
    {
        var order = CreateOpenOrder();
        var query = new GetOrderByIdQuery(CustomerOrderId: order.Id);
        var partialOne = CreatePartialPayment(20m);
        var partialTwo = CreatePartialPayment(15.50m);
        _orderRepository.GetByIdAsync(query.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _partialPaymentRepository.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns([partialOne, partialTwo]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PartialPaidAmount.Should().Be(35.50m);
        result.Value.TotalAmount.Should().Be(order.TotalAmount);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
