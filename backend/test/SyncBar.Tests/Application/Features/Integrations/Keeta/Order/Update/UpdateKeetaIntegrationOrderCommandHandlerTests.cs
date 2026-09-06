using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Order.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Update;

public sealed class UpdateKeetaIntegrationOrderCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateKeetaIntegrationOrderCommandHandler _handler;

    public UpdateKeetaIntegrationOrderCommandHandlerTests()
    {
        _handler = new UpdateKeetaIntegrationOrderCommandHandler(_orderRepository, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrder MakeOrder(long companyId = 1) =>
        KeetaIntegrationOrder.Create(companyId, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateKeetaIntegrationOrderCommand(1, 1, "CONFIRMED");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var order = MakeOrder(1);
        var command = new UpdateKeetaIntegrationOrderCommand(1, 2, "CONFIRMED");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_StatusConfirmed_ShouldMarkAsConfirmed()
    {
        var order = MakeOrder(1);
        var command = new UpdateKeetaIntegrationOrderCommand(1, 1, "CONFIRMED");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("CONFIRMED");
        order.ConfirmedAtUtc.Should().NotBeNull();
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StatusReadyForPickup_ShouldMarkAsReadyForPickup()
    {
        var order = MakeOrder(1);
        var command = new UpdateKeetaIntegrationOrderCommand(1, 1, "READY_FOR_PICKUP");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("READY_FOR_PICKUP");
        order.ReadyForPickupAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_StatusConcluded_ShouldMarkAsConcluded()
    {
        var order = MakeOrder(1);
        var command = new UpdateKeetaIntegrationOrderCommand(1, 1, "CONCLUDED");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("CONCLUDED");
        order.ConcludedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_UnknownStatus_ShouldChangeStatusGenerically()
    {
        var order = MakeOrder(1);
        var command = new UpdateKeetaIntegrationOrderCommand(1, 1, "DISPATCHED");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("DISPATCHED");
    }

    [Fact]
    public async Task Handle_NoStatusProvided_ShouldNotChangeStatus()
    {
        var order = MakeOrder(1);
        var command = new UpdateKeetaIntegrationOrderCommand(1, 1);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("CREATED");
    }
}
