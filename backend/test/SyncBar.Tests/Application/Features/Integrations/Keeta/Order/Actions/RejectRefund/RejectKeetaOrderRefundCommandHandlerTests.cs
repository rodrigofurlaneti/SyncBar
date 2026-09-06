using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RejectRefund;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.RejectRefund;

public sealed class RejectKeetaOrderRefundCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderRepository _orderRepository = Substitute.For<IKeetaIntegrationOrderRepository>();
    private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository = Substitute.For<IKeetaIntegrationRefundDisputeRepository>();
    private readonly IKeetaOrderClient _orderClient = Substitute.For<IKeetaOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RejectKeetaOrderRefundCommandHandler _handler;

    public RejectKeetaOrderRefundCommandHandlerTests()
    {
        _handler = new RejectKeetaOrderRefundCommandHandler(_orderRepository, _disputeRepository, _orderClient, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrder MakeOrder() =>
        KeetaIntegrationOrder.Create(1, 2, 3, 4, "keeta-order-1", "display-1", "im-1", 100, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnNotFound()
    {
        var command = new RejectKeetaOrderRefundCommand(1, "prato já entregue", "OUT_FOR_DELIVERY");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrder.NotFound");
    }

    [Fact]
    public async Task Handle_NoAssociatedDispute_ShouldStillSucceedWithoutResolvingDispute()
    {
        var order = MakeOrder();
        var command = new RejectKeetaOrderRefundCommand(1, "prato já entregue", "OUT_FOR_DELIVERY");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _disputeRepository.GetByOrderIdAsync(order.KeetaOrderId, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationRefundDispute?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be("REFUND_REJECTED");
        await _orderClient.Received(1).RejectRefundAsync(
            order.CompanyId, order.BranchId, order.KeetaOrderId, "prato já entregue", "OUT_FOR_DELIVERY", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AssociatedDisputeExists_ShouldResolveDisputeAsRejectedWithReason()
    {
        var order = MakeOrder();
        var command = new RejectKeetaOrderRefundCommand(1, "prato já entregue", "OUT_FOR_DELIVERY");
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var dispute = KeetaIntegrationRefundDispute.Create(1, 2, order.KeetaOrderId, 500, 20m, "motivo").Value;
        _disputeRepository.GetByOrderIdAsync(order.KeetaOrderId, Arg.Any<CancellationToken>()).Returns(dispute);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dispute.ResolutionStatus.Should().Be("REJECTED");
        dispute.DenialReasonCode.Should().Be("OUT_FOR_DELIVERY");
        dispute.DenialReasonText.Should().Be("prato já entregue");
        _disputeRepository.Received(1).Update(dispute);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
