using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByCompanyId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.GetByCompanyId;

public sealed class GetKeetaSettingByCompanyIdQueryValidatorTests
{
    private readonly GetKeetaSettingByCompanyIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new GetKeetaSettingByCompanyIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetKeetaSettingByCompanyIdQuery(companyId)).IsValid.Should().BeFalse();
}
