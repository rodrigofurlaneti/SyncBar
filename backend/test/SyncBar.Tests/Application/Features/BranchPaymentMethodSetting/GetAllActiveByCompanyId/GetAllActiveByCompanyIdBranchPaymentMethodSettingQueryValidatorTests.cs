using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActiveByCompanyId;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetAllActiveByCompanyId;

public sealed class GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryValidatorTests
{
    private readonly GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new GetAllActiveByCompanyIdBranchPaymentMethodSettingQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAllActiveByCompanyIdBranchPaymentMethodSettingQuery(companyId)).IsValid.Should().BeFalse();
}
