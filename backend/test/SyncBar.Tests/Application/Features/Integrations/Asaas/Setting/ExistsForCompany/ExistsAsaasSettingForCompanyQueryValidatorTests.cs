using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForCompany;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.ExistsForCompany;

public sealed class ExistsAsaasSettingForCompanyQueryValidatorTests
{
    private readonly ExistsAsaasSettingForCompanyQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new ExistsAsaasSettingForCompanyQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new ExistsAsaasSettingForCompanyQuery(companyId)).IsValid.Should().BeFalse();
}
