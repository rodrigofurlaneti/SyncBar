using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByOrderId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.GetByOrderId;

public sealed class GetKeetaRefundDisputeByOrderIdQueryValidatorTests
{
    private readonly GetKeetaRefundDisputeByOrderIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyOrderId_ShouldBeValid()
        => _validator.Validate(new GetKeetaRefundDisputeByOrderIdQuery("order-1")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyOrderId_ShouldBeInvalid()
        => _validator.Validate(new GetKeetaRefundDisputeByOrderIdQuery(string.Empty)).IsValid.Should().BeFalse();
}
