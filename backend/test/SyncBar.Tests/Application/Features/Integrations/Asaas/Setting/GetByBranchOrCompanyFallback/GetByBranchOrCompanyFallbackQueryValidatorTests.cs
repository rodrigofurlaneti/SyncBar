using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback;

public sealed class GetByBranchOrCompanyFallbackQueryValidatorTests
{
    private readonly GetByBranchOrCompanyFallbackQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQueryWithoutBranchId_ShouldBeValid()
        => _validator.Validate(new GetByBranchOrCompanyFallbackQuery(1)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidQueryWithBranchId_ShouldBeValid()
        => _validator.Validate(new GetByBranchOrCompanyFallbackQuery(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetByBranchOrCompanyFallbackQuery(companyId)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchIdWhenProvided_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetByBranchOrCompanyFallbackQuery(1, branchId)).IsValid.Should().BeFalse();
}
