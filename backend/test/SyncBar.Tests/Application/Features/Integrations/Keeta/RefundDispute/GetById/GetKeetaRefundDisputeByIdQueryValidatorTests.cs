using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.GetById;

public sealed class GetKeetaRefundDisputeByIdQueryValidatorTests
{
    private readonly GetKeetaRefundDisputeByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetKeetaRefundDisputeByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetKeetaRefundDisputeByIdQuery(id)).IsValid.Should().BeFalse();
}
