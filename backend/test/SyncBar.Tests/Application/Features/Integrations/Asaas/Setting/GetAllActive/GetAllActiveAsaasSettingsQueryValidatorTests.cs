using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetAllActive;

public sealed class GetAllActiveAsaasSettingsQueryValidatorTests
{
    private readonly GetAllActiveAsaasSettingsQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new GetAllActiveAsaasSettingsQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAllActiveAsaasSettingsQuery(companyId)).IsValid.Should().BeFalse();
}
