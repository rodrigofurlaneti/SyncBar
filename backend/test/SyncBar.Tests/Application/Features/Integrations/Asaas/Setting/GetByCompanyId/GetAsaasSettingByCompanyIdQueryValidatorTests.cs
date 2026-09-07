using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByCompanyId;

public sealed class GetAsaasSettingByCompanyIdQueryValidatorTests
{
    private readonly GetAsaasSettingByCompanyIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new GetAsaasSettingByCompanyIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAsaasSettingByCompanyIdQuery(companyId)).IsValid.Should().BeFalse();
}
