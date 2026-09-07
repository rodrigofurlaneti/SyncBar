using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForBranch;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.ExistsForBranch;

public sealed class ExistsKeetaSettingForBranchQueryValidatorTests
{
    private readonly ExistsKeetaSettingForBranchQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new ExistsKeetaSettingForBranchQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new ExistsKeetaSettingForBranchQuery(branchId)).IsValid.Should().BeFalse();
}
