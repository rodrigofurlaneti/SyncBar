using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByOrderId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.GetByOrderId;

public sealed class GetKeetaRefundDisputeByOrderIdQueryHandlerTests
{
    private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository = Substitute.For<IKeetaIntegrationRefundDisputeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaRefundDisputeByOrderIdQueryHandler _handler;

    public GetKeetaRefundDisputeByOrderIdQueryHandlerTests()
    {
        _handler = new GetKeetaRefundDisputeByOrderIdQueryHandler(_disputeRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_DisputeNotFound_ShouldReturnNotFound()
    {
        _disputeRepository.GetByOrderIdAsync("order-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationRefundDispute?)null);

        var result = await _handler.Handle(new GetKeetaRefundDisputeByOrderIdQuery("order-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaRefundDispute.NotFound");
    }

    [Fact]
    public async Task Handle_DisputeFound_ShouldReturnMappedResponse()
    {
        var dispute = KeetaIntegrationRefundDispute.Create(1, 2, "order-1", 500, 20m, "motivo").Value;
        _disputeRepository.GetByOrderIdAsync("order-1", Arg.Any<CancellationToken>()).Returns(dispute);

        var result = await _handler.Handle(new GetKeetaRefundDisputeByOrderIdQuery("order-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be("order-1");
    }
}
