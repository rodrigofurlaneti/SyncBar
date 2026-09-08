using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.Create;

public sealed class CreateBranchPaymentMethodSettingCommandValidatorTests
{
    private readonly CreateBranchPaymentMethodSettingCommandValidator _validator = new();

    private static CreateBranchPaymentMethodSettingCommand Valid() => new(CompanyId: 1, BranchId: 2);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithoutBranchId_ShouldBeValid()
        => _validator.Validate(Valid() with { BranchId = null }).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(Valid() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchIdWhenProvided_ShouldBeInvalid(long branchId)
        => _validator.Validate(Valid() with { BranchId = branchId }).IsValid.Should().BeFalse();
}
