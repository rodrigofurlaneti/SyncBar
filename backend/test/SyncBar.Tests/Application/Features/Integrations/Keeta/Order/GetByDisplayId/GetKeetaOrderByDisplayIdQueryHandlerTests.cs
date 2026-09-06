using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetByDisplayId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetByDisplayId;

public sealed class GetKeetaOrderByDisplayIdQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaOrderByDisplayIdQueryHandler _handler;

    public GetKeetaOrderByDisplayIdQueryHandlerTests()
    {
        _handler = new GetKeetaOrderByDisplayIdQueryHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        _orderRepository.GetByDisplayIdAsync("display-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(new GetKeetaOrderByDisplayIdQuery("display-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_OrderFound_ShouldReturnMappedResponse()
    {
        var order = KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;
        _orderRepository.GetByDisplayIdAsync("display-1", Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(new GetKeetaOrderByDisplayIdQuery("display-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayId.Should().Be("display-1");
    }
}
