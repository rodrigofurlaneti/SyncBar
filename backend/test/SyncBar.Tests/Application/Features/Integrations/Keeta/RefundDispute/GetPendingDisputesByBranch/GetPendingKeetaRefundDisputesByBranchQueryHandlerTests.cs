using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetPendingDisputesByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.GetPendingDisputesByBranch;

public sealed class GetPendingKeetaRefundDisputesByBranchQueryHandlerTests
{
    private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository = Substitute.For<IKeetaIntegrationRefundDisputeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPendingKeetaRefundDisputesByBranchQueryHandler _handler;

    public GetPendingKeetaRefundDisputesByBranchQueryHandlerTests()
    {
        _handler = new GetPendingKeetaRefundDisputesByBranchQueryHandler(_disputeRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_PendingDisputesExist_ShouldReturnMappedList()
    {
        var disputes = new List<KeetaIntegrationRefundDispute>
        {
            KeetaIntegrationRefundDispute.Create(1, 2, "order-1", 500, 20m, "motivo").Value,
        };
        _disputeRepository.GetPendingDisputesByBranchAsync(2, Arg.Any<CancellationToken>()).Returns(disputes);

        var result = await _handler.Handle(new GetPendingKeetaRefundDisputesByBranchQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoPendingDisputes_ShouldReturnEmptyList()
    {
        _disputeRepository.GetPendingDisputesByBranchAsync(2, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetPendingKeetaRefundDisputesByBranchQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
