using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByIdForUpdate;

public sealed class GetAsaasSettingByIdForUpdateQueryValidatorTests
{
    private readonly GetAsaasSettingByIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetAsaasSettingByIdForUpdateQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetAsaasSettingByIdForUpdateQuery(id)).IsValid.Should().BeFalse();
}
