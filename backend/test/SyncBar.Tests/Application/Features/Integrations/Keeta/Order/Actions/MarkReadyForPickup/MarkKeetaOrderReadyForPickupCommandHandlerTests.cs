using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkReadyForPickup;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.MarkReadyForPickup;

public sealed class MarkKeetaOrderReadyForPickupCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly IKeetaOrderClient _orderClient = Substitute.For<IKeetaOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly MarkKeetaOrderReadyForPickupCommandHandler _handler;

    public MarkKeetaOrderReadyForPickupCommandHandlerTests()
    {
        _handler = new MarkKeetaOrderReadyForPickupCommandHandler(_orderRepository, _orderClient, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrder MakeOrder() =>
        KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var command = new MarkKeetaOrderReadyForPickupCommand(1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallKeetaApiAndMarkAsReadyForPickup()
    {
        var order = MakeOrder();
        var command = new MarkKeetaOrderReadyForPickupCommand(1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("READY_FOR_PICKUP");
        order.ReadyForPickupAtUtc.Should().NotBeNull();
        await _orderClient.Received(1).MarkReadyForPickupAsync(order.CompanyId, order.BranchId, order.KeetaOrderId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
