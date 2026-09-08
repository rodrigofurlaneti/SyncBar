using FluentAssertions;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.GetById;

public sealed class GetByIdBranchPaymentMethodSettingQueryValidatorTests
{
    private readonly GetByIdBranchPaymentMethodSettingQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetByIdBranchPaymentMethodSettingQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetByIdBranchPaymentMethodSettingQuery(id)).IsValid.Should().BeFalse();
}
