using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByBranchId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByBranchId;

public sealed class GetAllKeetaMerchantMappingsByBranchIdQueryValidatorTests
{
    private readonly GetAllKeetaMerchantMappingsByBranchIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetAllKeetaMerchantMappingsByBranchIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetAllKeetaMerchantMappingsByBranchIdQuery(branchId)).IsValid.Should().BeFalse();
}
