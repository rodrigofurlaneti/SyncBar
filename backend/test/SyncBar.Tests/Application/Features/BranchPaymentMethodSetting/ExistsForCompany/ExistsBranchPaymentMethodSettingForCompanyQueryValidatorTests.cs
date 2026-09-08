using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForCompany;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.ExistsForCompany;

public sealed class ExistsBranchPaymentMethodSettingForCompanyQueryValidatorTests
{
    private readonly ExistsBranchPaymentMethodSettingForCompanyQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new ExistsBranchPaymentMethodSettingForCompanyQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new ExistsBranchPaymentMethodSettingForCompanyQuery(companyId)).IsValid.Should().BeFalse();
}
