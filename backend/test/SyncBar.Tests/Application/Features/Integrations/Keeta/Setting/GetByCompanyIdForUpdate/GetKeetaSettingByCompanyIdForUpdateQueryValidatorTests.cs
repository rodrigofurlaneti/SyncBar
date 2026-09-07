using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByCompanyIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByCompanyIdForUpdate;

public sealed class GetKeetaSettingByCompanyIdForUpdateQueryValidatorTests
{
    private readonly GetKeetaSettingByCompanyIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByCompanyIdForUpdateQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetKeetaSettingByCompanyIdForUpdateQuery(companyId)).IsValid.Should().BeFalse();
}
