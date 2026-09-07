using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetPendingDisputesByBranch;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.GetPendingDisputesByBranch;

public sealed class GetPendingKeetaRefundDisputesByBranchQueryValidatorTests
{
    private readonly GetPendingKeetaRefundDisputesByBranchQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetPendingKeetaRefundDisputesByBranchQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetPendingKeetaRefundDisputesByBranchQuery(branchId)).IsValid.Should().BeFalse();
}
