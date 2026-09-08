using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetAllActive;

public sealed class GetAllActiveBranchPaymentMethodSettingQueryValidatorTests
{
    private readonly GetAllActiveBranchPaymentMethodSettingQueryValidator _validator = new();

    [Fact]
    public void Validate_QueryWithoutParameters_ShouldAlwaysBeValid()
        => _validator.Validate(new GetAllActiveBranchPaymentMethodSettingQuery()).IsValid.Should().BeTrue();
}
