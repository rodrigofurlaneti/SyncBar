using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetActiveOrdersByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetActiveOrdersByBranch;

public sealed class GetActiveKeetaOrdersByBranchQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetActiveKeetaOrdersByBranchQueryHandler _handler;

    public GetActiveKeetaOrdersByBranchQueryHandlerTests()
    {
        _handler = new GetActiveKeetaOrdersByBranchQueryHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ActiveOrdersExist_ShouldReturnMappedList()
    {
        var orders = new List<KeetaIntegrationOrder>
        {
            KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value,
        };
        _orderRepository.GetActiveOrdersByBranchAsync(2, Arg.Any<CancellationToken>()).Returns(orders);

        var result = await _handler.Handle(new GetActiveKeetaOrdersByBranchQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoActiveOrders_ShouldReturnEmptyList()
    {
        _orderRepository.GetActiveOrdersByBranchAsync(2, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetActiveKeetaOrdersByBranchQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
