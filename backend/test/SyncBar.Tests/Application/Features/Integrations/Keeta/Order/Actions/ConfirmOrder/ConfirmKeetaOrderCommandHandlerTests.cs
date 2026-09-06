using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.ConfirmOrder;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.ConfirmOrder;

public sealed class ConfirmKeetaOrderCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly IKeetaOrderClient _orderClient = Substitute.For<IKeetaOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ConfirmKeetaOrderCommandHandler _handler;

    public ConfirmKeetaOrderCommandHandlerTests()
    {
        _handler = new ConfirmKeetaOrderCommandHandler(_orderRepository, _orderClient, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrder MakeOrder() =>
        KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var command = new ConfirmKeetaOrderCommand(1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
        await _orderClient.DidNotReceive().ConfirmOrderAsync(
            Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(),
            Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallKeetaApiAndMarkAsConfirmed()
    {
        var order = MakeOrder();
        var command = new ConfirmKeetaOrderCommand(1, "aceito pelo garçom", 20);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("CONFIRMED");
        order.ConfirmedAtUtc.Should().NotBeNull();
        await _orderClient.Received(1).ConfirmOrderAsync(
            order.CompanyId, order.BranchId, order.KeetaOrderId, order.DisplayId, order.OrderCreatedAtUtc,
            "aceito pelo garçom", 20, Arg.Any<CancellationToken>());
        _orderRepository.Received(1).Update(order);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
