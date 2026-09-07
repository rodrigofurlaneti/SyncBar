using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForBranch;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.ExistsForBranch;

public sealed class ExistsAsaasSettingForBranchQueryValidatorTests
{
    private readonly ExistsAsaasSettingForBranchQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(new ExistsAsaasSettingForBranchQuery(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new ExistsAsaasSettingForBranchQuery(companyId, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new ExistsAsaasSettingForBranchQuery(1, branchId)).IsValid.Should().BeFalse();
}
