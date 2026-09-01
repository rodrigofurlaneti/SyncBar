using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.RaiseComandaLimit;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.RaiseComandaLimit;

public sealed class RaiseComandaLimitCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RaiseComandaLimitCommandHandler _handler;

    public RaiseComandaLimitCommandHandlerTests()
    {
        _handler = new RaiseComandaLimitCommandHandler(
            _orderRepository, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateTableOrder()
        => CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;

    private static CustomerOrder CreateComandaOrder(decimal? creditLimitAmount = null)
        => CustomerOrder.Create(1, null, 20, 1, null, null, DateTime.Now, creditLimitAmount: creditLimitAmount).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new RaiseComandaLimitCommand(CustomerOrderId: 1, NewLimitAmount: 200m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TableOrder_ShouldReturnFailure()
    {
        var order = CreateTableOrder();
        var command = new RaiseComandaLimitCommand(CustomerOrderId: 1, NewLimitAmount: 200m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.LimitTableOrder");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewLimitNotGreaterThanCurrent_ShouldReturnFailure()
    {
        var order = CreateComandaOrder(creditLimitAmount: 100m);
        var command = new RaiseComandaLimitCommand(CustomerOrderId: 1, NewLimitAmount: 100m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.LimitMustIncrease");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComandaOrderWithHigherLimit_ShouldRaiseLimitAndCommitTwice()
    {
        var order = CreateComandaOrder(creditLimitAmount: 100m);
        var command = new RaiseComandaLimitCommand(CustomerOrderId: 1, NewLimitAmount: 250m);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.CreditLimitAmount.Should().Be(250m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
