using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId;

public sealed class ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryHandlerTests
{
    private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository = Substitute.For<IKeetaIntegrationRefundDisputeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryHandler _handler;

    public ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryHandlerTests()
    {
        _handler = new ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryHandler(_disputeRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_DisputeExists_ShouldReturnTrue()
    {
        _disputeRepository.ExistsByAfterSaleOrderIdAsync(500, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery(500), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DisputeDoesNotExist_ShouldReturnFalse()
    {
        _disputeRepository.ExistsByAfterSaleOrderIdAsync(500, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery(500), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
