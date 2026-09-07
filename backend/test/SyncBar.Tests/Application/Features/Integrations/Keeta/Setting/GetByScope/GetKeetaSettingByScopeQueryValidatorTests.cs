using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByScope;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByScope;

public sealed class GetKeetaSettingByScopeQueryValidatorTests
{
    private readonly GetKeetaSettingByScopeQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyIdWithBranchId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByScopeQuery(1, 1)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_PositiveCompanyIdWithNullBranchId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByScopeQuery(1, null)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetKeetaSettingByScopeQuery(companyId, 1)).IsValid.Should().BeFalse();
}
