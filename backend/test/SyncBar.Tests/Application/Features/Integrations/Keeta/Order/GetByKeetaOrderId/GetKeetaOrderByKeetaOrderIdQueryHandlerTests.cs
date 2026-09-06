using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetByKeetaOrderId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetByKeetaOrderId;

public sealed class GetKeetaOrderByKeetaOrderIdQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaOrderByKeetaOrderIdQueryHandler _handler;

    public GetKeetaOrderByKeetaOrderIdQueryHandlerTests()
    {
        _handler = new GetKeetaOrderByKeetaOrderIdQueryHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        _orderRepository.GetByKeetaOrderIdAsync("keeta-order-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(new GetKeetaOrderByKeetaOrderIdQuery("keeta-order-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_OrderFound_ShouldReturnMappedResponse()
    {
        var order = KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;
        _orderRepository.GetByKeetaOrderIdAsync("keeta-order-1", Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(new GetKeetaOrderByKeetaOrderIdQuery("keeta-order-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.KeetaOrderId.Should().Be("keeta-order-1");
    }
}
