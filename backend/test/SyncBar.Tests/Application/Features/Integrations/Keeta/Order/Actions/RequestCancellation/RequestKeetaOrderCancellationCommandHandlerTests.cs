using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RequestCancellation;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.RequestCancellation;

public sealed class RequestKeetaOrderCancellationCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly IKeetaOrderClient _orderClient = Substitute.For<IKeetaOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RequestKeetaOrderCancellationCommandHandler _handler;

    public RequestKeetaOrderCancellationCommandHandlerTests()
    {
        _handler = new RequestKeetaOrderCancellationCommandHandler(_orderRepository, _orderClient, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrder MakeOrder() =>
        KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var command = new RequestKeetaOrderCancellationCommand(1, "sem entregador", "RESTAURANT_WITHOUT_DELIVERY_PERSON", "MANUAL");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallKeetaApiAndSetCancellationRequestedStatus()
    {
        var order = MakeOrder();
        var command = new RequestKeetaOrderCancellationCommand(
            1, "sem entregador", "RESTAURANT_WITHOUT_DELIVERY_PERSON", "MANUAL", ["item-1"], ["item-2"]);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("CANCELLATION_REQUESTED");
        await _orderClient.Received(1).RequestCancellationAsync(
            order.CompanyId, order.BranchId, order.KeetaOrderId,
            "sem entregador", "RESTAURANT_WITHOUT_DELIVERY_PERSON", "MANUAL",
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "item-1"),
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "item-2"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
