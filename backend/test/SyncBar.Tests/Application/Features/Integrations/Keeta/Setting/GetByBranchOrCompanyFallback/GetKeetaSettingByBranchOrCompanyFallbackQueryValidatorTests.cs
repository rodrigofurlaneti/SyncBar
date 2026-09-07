using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchOrCompanyFallback;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByBranchOrCompanyFallback;

public sealed class GetKeetaSettingByBranchOrCompanyFallbackQueryValidatorTests
{
    private readonly GetKeetaSettingByBranchOrCompanyFallbackQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyIdWithBranchId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByBranchOrCompanyFallbackQuery(1, 1)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_PositiveCompanyIdWithNullBranchId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByBranchOrCompanyFallbackQuery(1, null)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetKeetaSettingByBranchOrCompanyFallbackQuery(companyId, 1)).IsValid.Should().BeFalse();
}
