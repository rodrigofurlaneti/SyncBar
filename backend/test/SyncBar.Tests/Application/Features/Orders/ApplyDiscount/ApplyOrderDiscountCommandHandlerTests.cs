using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.ApplyDiscount;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.ApplyDiscount;

public sealed class ApplyOrderDiscountCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ApplyOrderDiscountCommandHandler _handler;

    public ApplyOrderDiscountCommandHandlerTests()
    {
        _handler = new ApplyOrderDiscountCommandHandler(
            _orderRepository, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOpenOrder()
        => CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new ApplyOrderDiscountCommand(CustomerOrderId: 1, DiscountAmount: 10m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderNotOpen_ShouldReturnFailure()
    {
        var order = CreateOpenOrder();
        order.Cancel(DateTime.Now);
        var command = new ApplyOrderDiscountCommand(CustomerOrderId: 1, DiscountAmount: 10m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeDiscount_ShouldReturnFailure()
    {
        var order = CreateOpenOrder();
        var command = new ApplyOrderDiscountCommand(CustomerOrderId: 1, DiscountAmount: -5m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.InvalidDiscount");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DiscountExceedsSubtotal_ShouldReturnFailure()
    {
        var order = CreateOpenOrder();
        var command = new ApplyOrderDiscountCommand(CustomerOrderId: 1, DiscountAmount: 10m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.DiscountExceedsSubtotal");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidDiscount_ShouldApplyDiscountAndCommitTwice()
    {
        var order = CreateOpenOrder();
        order.AddItem(productId: 1, unitPrice: 50m, quantity: 2, notes: null, employeeId: null, DateTime.Now);
        var command = new ApplyOrderDiscountCommand(CustomerOrderId: 1, DiscountAmount: 20m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(20m);
        order.TotalAmount.Should().Be(80m);
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
