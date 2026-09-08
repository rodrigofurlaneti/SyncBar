using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.Update;

public sealed class UpdateBranchPaymentMethodSettingCommandValidatorTests
{
    private readonly UpdateBranchPaymentMethodSettingCommandValidator _validator = new();

    private static UpdateBranchPaymentMethodSettingCommand Valid() => new(Id: 1, CompanyId: 1);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(Valid() with { Id = id }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(Valid() with { CompanyId = companyId }).IsValid.Should().BeFalse();
}
