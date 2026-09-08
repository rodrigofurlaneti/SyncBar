using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByCompanyId;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetByCompanyId;

public sealed class GetByCompanyIdBranchPaymentMethodSettingQueryValidatorTests
{
    private readonly GetByCompanyIdBranchPaymentMethodSettingQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new GetByCompanyIdBranchPaymentMethodSettingQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetByCompanyIdBranchPaymentMethodSettingQuery(companyId)).IsValid.Should().BeFalse();
}
