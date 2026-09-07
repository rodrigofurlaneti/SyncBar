using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByBranchId;

public sealed class GetKeetaSettingByBranchIdQueryValidatorTests
{
    private readonly GetKeetaSettingByBranchIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByBranchIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetKeetaSettingByBranchIdQuery(branchId)).IsValid.Should().BeFalse();
}
