using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.SplitBill;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.SplitBill;

public sealed class CalculateBillSplitQueryHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CalculateBillSplitQueryHandler _handler;

    public CalculateBillSplitQueryHandlerTests()
    {
        _handler = new CalculateBillSplitQueryHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOrderWithTotal(decimal itemUnitPrice, long branchId = 1, long diningTableId = 1, long employeeId = 1)
    {
        var order = CustomerOrder.Create(branchId, diningTableId, null, employeeId, 4, null, DateTime.Now).Value;
        order.AddItem(1, itemUnitPrice, 1, null, employeeId, DateTime.Now);
        return order;
    }

    [Fact]
    public async Task Handle_PeopleCountZero_ShouldReturnInvalidPeopleCountWithoutCallingOrderRepository()
    {
        var query = new CalculateBillSplitQuery(CustomerOrderId: 1, PeopleCount: 0);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillSplit.InvalidPeopleCount");
        await _orderRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PeopleCountNegative_ShouldReturnInvalidPeopleCountWithoutCallingOrderRepository()
    {
        var query = new CalculateBillSplitQuery(CustomerOrderId: 1, PeopleCount: -3);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillSplit.InvalidPeopleCount");
        await _orderRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnCustomerOrderNotFound()
    {
        var query = new CalculateBillSplitQuery(CustomerOrderId: 99, PeopleCount: 2);
        _orderRepository.GetByIdAsync(query.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderInactive_ShouldReturnCustomerOrderNotFound()
    {
        var order = CreateOrderWithTotal(100m);
        order.Deactivate(DateTime.Now);
        var query = new CalculateBillSplitQuery(CustomerOrderId: order.Id, PeopleCount: 2);
        _orderRepository.GetByIdAsync(query.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
    }

    [Fact]
    public async Task Handle_EvenSplitAmongFourPeople_ShouldReturnEqualShares()
    {
        var order = CreateOrderWithTotal(100m);
        var query = new CalculateBillSplitQuery(CustomerOrderId: order.Id, PeopleCount: 4);
        _orderRepository.GetByIdAsync(query.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().Be(order.TotalAmount);
        result.Value.PeopleCount.Should().Be(4);
        result.Value.Shares.Should().HaveCount(4);
        result.Value.Shares.Should().OnlyContain(s => s.Amount == 25.00m);
        result.Value.Shares.Sum(s => s.Amount).Should().Be(order.TotalAmount);
    }

    [Fact]
    public async Task Handle_SplitWithRemainder_ShouldGiveExtraCentToFirstPeopleAndLoseNoCents()
    {
        // R$10,00 / 3 pessoas = 1000 centavos / 3 -> base 333, resto 1: pessoa 1 recebe 334,
        // pessoas 2 e 3 recebem 333 cada. Soma das partes = R$10,00 exatos, sem sobra/perda.
        var order = CreateOrderWithTotal(10.00m);
        var query = new CalculateBillSplitQuery(CustomerOrderId: order.Id, PeopleCount: 3);
        _orderRepository.GetByIdAsync(query.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Shares.Should().HaveCount(3);
        result.Value.Shares.First(s => s.PersonNumber == 1).Amount.Should().Be(3.34m);
        result.Value.Shares.First(s => s.PersonNumber == 2).Amount.Should().Be(3.33m);
        result.Value.Shares.First(s => s.PersonNumber == 3).Amount.Should().Be(3.33m);
        result.Value.Shares.Sum(s => s.Amount).Should().Be(order.TotalAmount);
    }
}
