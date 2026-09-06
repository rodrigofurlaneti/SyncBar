using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.SendTrackingUpdate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.SendTrackingUpdate;

public sealed class SendKeetaOrderTrackingUpdateCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly IKeetaOrderClient _orderClient = Substitute.For<IKeetaOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SendKeetaOrderTrackingUpdateCommandHandler _handler;

    public SendKeetaOrderTrackingUpdateCommandHandlerTests()
    {
        _handler = new SendKeetaOrderTrackingUpdateCommandHandler(_orderRepository, _orderClient, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrder MakeOrder() =>
        KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var command = new SendKeetaOrderTrackingUpdateCommand(1, "ARRIVED_AT_CUSTOMER");
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldSendTrackingUpdateWithoutMutatingLocalOrder()
    {
        var order = MakeOrder();
        var command = new SendKeetaOrderTrackingUpdateCommand(1, "ARRIVED_AT_CUSTOMER", "chegou no cliente");
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.Received(1).SendTrackingUpdateAsync(
            order.CompanyId, order.BranchId, order.KeetaOrderId,
            Arg.Is<KeetaDeliveryTrackingEvent>(e => e.Type == "ARRIVED_AT_CUSTOMER" && e.Message == "chegou no cliente"),
            Arg.Any<CancellationToken>());
        _orderRepository.DidNotReceive().Update(Arg.Any<KeetaIntegrationOrder>());
    }
}
