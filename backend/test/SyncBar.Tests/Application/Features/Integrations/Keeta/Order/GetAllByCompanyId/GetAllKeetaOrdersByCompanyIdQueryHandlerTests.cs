using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByCompanyId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetAllByCompanyId;

public sealed class GetAllKeetaOrdersByCompanyIdQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllKeetaOrdersByCompanyIdQueryHandler _handler;

    public GetAllKeetaOrdersByCompanyIdQueryHandlerTests()
    {
        _handler = new GetAllKeetaOrdersByCompanyIdQueryHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_OrdersExist_ShouldReturnMappedList()
    {
        var orders = new List<KeetaIntegrationOrder>
        {
            KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value,
        };
        _orderRepository.GetAllByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns(orders);

        var result = await _handler.Handle(new GetAllKeetaOrdersByCompanyIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoOrdersForCompany_ShouldReturnEmptyList()
    {
        _orderRepository.GetAllByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetAllKeetaOrdersByCompanyIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
