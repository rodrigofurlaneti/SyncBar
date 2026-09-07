using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByCompanyIdForUpdate;

public sealed class GetAsaasSettingByCompanyIdForUpdateQueryValidatorTests
{
    private readonly GetAsaasSettingByCompanyIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new GetAsaasSettingByCompanyIdForUpdateQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAsaasSettingByCompanyIdForUpdateQuery(companyId)).IsValid.Should().BeFalse();
}
