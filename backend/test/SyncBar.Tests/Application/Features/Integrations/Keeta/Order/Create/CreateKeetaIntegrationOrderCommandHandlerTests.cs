using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Order.Create;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Create;

public sealed class CreateKeetaIntegrationOrderCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateKeetaIntegrationOrderCommandHandler _handler;

    public CreateKeetaIntegrationOrderCommandHandlerTests()
    {
        _handler = new CreateKeetaIntegrationOrderCommandHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    private static CreateKeetaIntegrationOrderCommand ValidCommand() =>
        new(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow);

    [Fact]
    public async Task Handle_KeetaOrderIdAlreadyExists_ShouldReturnConflict()
    {
        var command = ValidCommand();
        _orderRepository.ExistsByKeetaOrderIdAsync("keeta-order-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.AlreadyExists");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnMappedResponse()
    {
        var command = ValidCommand();
        _orderRepository.ExistsByKeetaOrderIdAsync("keeta-order-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.KeetaOrderId.Should().Be("keeta-order-1");
        result.Value.Status.Should().Be("CREATED");
        result.Value.OrderAmount.Should().Be(50m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
