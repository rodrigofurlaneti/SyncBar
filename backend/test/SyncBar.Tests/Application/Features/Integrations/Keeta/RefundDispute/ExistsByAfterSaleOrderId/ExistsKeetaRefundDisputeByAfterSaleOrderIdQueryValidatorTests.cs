using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId;

public sealed class ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryValidatorTests
{
    private readonly ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveAfterSaleOrderId_ShouldBeValid()
        => _validator.Validate(new ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveAfterSaleOrderId_ShouldBeInvalid(long afterSaleOrderId)
        => _validator.Validate(new ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery(afterSaleOrderId)).IsValid.Should().BeFalse();
}
