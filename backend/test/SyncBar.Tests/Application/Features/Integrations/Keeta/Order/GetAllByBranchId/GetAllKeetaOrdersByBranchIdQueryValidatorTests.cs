using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByBranchId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetAllByBranchId;

public sealed class GetAllKeetaOrdersByBranchIdQueryValidatorTests
{
    private readonly GetAllKeetaOrdersByBranchIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetAllKeetaOrdersByBranchIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetAllKeetaOrdersByBranchIdQuery(branchId)).IsValid.Should().BeFalse();
}
