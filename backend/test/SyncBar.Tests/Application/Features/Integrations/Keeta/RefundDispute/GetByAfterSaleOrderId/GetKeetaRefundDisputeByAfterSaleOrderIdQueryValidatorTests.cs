using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByAfterSaleOrderId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.GetByAfterSaleOrderId;

public sealed class GetKeetaRefundDisputeByAfterSaleOrderIdQueryValidatorTests
{
    private readonly GetKeetaRefundDisputeByAfterSaleOrderIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveAfterSaleOrderId_ShouldBeValid()
        => _validator.Validate(new GetKeetaRefundDisputeByAfterSaleOrderIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveAfterSaleOrderId_ShouldBeInvalid(long afterSaleOrderId)
        => _validator.Validate(new GetKeetaRefundDisputeByAfterSaleOrderIdQuery(afterSaleOrderId)).IsValid.Should().BeFalse();
}
