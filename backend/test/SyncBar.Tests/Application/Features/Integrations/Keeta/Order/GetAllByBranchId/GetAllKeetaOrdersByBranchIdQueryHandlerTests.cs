using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByBranchId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetAllByBranchId;

public sealed class GetAllKeetaOrdersByBranchIdQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllKeetaOrdersByBranchIdQueryHandler _handler;

    public GetAllKeetaOrdersByBranchIdQueryHandlerTests()
    {
        _handler = new GetAllKeetaOrdersByBranchIdQueryHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_OrdersExist_ShouldReturnMappedList()
    {
        var orders = new List<KeetaIntegrationOrder>
        {
            KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value,
        };
        _orderRepository.GetAllByBranchIdAsync(2, Arg.Any<CancellationToken>()).Returns(orders);

        var result = await _handler.Handle(new GetAllKeetaOrdersByBranchIdQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoOrdersForBranch_ShouldReturnEmptyList()
    {
        _orderRepository.GetAllByBranchIdAsync(2, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetAllKeetaOrdersByBranchIdQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
