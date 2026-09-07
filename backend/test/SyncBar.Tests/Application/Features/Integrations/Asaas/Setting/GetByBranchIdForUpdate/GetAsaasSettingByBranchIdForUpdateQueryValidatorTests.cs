using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByBranchIdForUpdate;

public sealed class GetAsaasSettingByBranchIdForUpdateQueryValidatorTests
{
    private readonly GetAsaasSettingByBranchIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(new GetAsaasSettingByBranchIdForUpdateQuery(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAsaasSettingByBranchIdForUpdateQuery(companyId, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetAsaasSettingByBranchIdForUpdateQuery(1, branchId)).IsValid.Should().BeFalse();
}
