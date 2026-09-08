using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForBranch;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.ExistsForBranch;

public sealed class ExistsBranchPaymentMethodSettingForBranchQueryValidatorTests
{
    private readonly ExistsBranchPaymentMethodSettingForBranchQueryValidator _validator = new();

    private static ExistsBranchPaymentMethodSettingForBranchQuery Valid() => new(CompanyId: 1, BranchId: 2);

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(Valid() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(Valid() with { BranchId = branchId }).IsValid.Should().BeFalse();
}
