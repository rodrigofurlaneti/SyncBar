using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForCompany;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.ExistsForCompany;

public sealed class ExistsKeetaSettingForCompanyQueryValidatorTests
{
    private readonly ExistsKeetaSettingForCompanyQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new ExistsKeetaSettingForCompanyQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new ExistsKeetaSettingForCompanyQuery(companyId)).IsValid.Should().BeFalse();
}
