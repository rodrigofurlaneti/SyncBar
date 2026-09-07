using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByBranchId;

public sealed class GetAsaasSettingByBranchIdQueryValidatorTests
{
    private readonly GetAsaasSettingByBranchIdQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(new GetAsaasSettingByBranchIdQuery(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAsaasSettingByBranchIdQuery(companyId, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetAsaasSettingByBranchIdQuery(1, branchId)).IsValid.Should().BeFalse();
}
