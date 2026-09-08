using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchId;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetByBranchId;

public sealed class GetByBranchIdBranchPaymentMethodSettingQueryValidatorTests
{
    private readonly GetByBranchIdBranchPaymentMethodSettingQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetByBranchIdBranchPaymentMethodSettingQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetByBranchIdBranchPaymentMethodSettingQuery(branchId)).IsValid.Should().BeFalse();
}
