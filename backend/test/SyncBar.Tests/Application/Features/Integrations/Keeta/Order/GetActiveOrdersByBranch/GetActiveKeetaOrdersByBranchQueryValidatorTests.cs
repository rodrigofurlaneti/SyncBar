using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetActiveOrdersByBranch;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetActiveOrdersByBranch;

public sealed class GetActiveKeetaOrdersByBranchQueryValidatorTests
{
    private readonly GetActiveKeetaOrdersByBranchQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetActiveKeetaOrdersByBranchQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetActiveKeetaOrdersByBranchQuery(branchId)).IsValid.Should().BeFalse();
}
