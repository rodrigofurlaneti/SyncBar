using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByBranchIdForUpdate;

public sealed class GetKeetaSettingByBranchIdForUpdateQueryValidatorTests
{
    private readonly GetKeetaSettingByBranchIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByBranchIdForUpdateQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetKeetaSettingByBranchIdForUpdateQuery(branchId)).IsValid.Should().BeFalse();
}
