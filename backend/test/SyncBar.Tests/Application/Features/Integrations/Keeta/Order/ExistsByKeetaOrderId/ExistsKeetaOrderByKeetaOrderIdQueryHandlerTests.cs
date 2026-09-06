using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Order.ExistsByKeetaOrderId;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.ExistsByKeetaOrderId;

public sealed class ExistsKeetaOrderByKeetaOrderIdQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsKeetaOrderByKeetaOrderIdQueryHandler _handler;

    public ExistsKeetaOrderByKeetaOrderIdQueryHandlerTests()
    {
        _handler = new ExistsKeetaOrderByKeetaOrderIdQueryHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_OrderExists_ShouldReturnTrue()
    {
        _orderRepository.ExistsByKeetaOrderIdAsync("keeta-order-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsKeetaOrderByKeetaOrderIdQuery("keeta-order-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrderDoesNotExist_ShouldReturnFalse()
    {
        _orderRepository.ExistsByKeetaOrderIdAsync("keeta-order-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsKeetaOrderByKeetaOrderIdQuery("keeta-order-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
