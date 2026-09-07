using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.Create;

public sealed class CreateKeetaIntegrationMerchantMappingCommandValidatorTests
{
    private readonly CreateKeetaIntegrationMerchantMappingCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new CreateKeetaIntegrationMerchantMappingCommand(1, 1, "internal-1", 1, "Store"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new CreateKeetaIntegrationMerchantMappingCommand(companyId, 1, "internal-1", 1, "Store"))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new CreateKeetaIntegrationMerchantMappingCommand(1, branchId, "internal-1", 1, "Store"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyInternalMerchantId_ShouldBeInvalid()
        => _validator.Validate(new CreateKeetaIntegrationMerchantMappingCommand(1, 1, string.Empty, 1, "Store"))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveKeetaMerchantId_ShouldBeInvalid(long keetaMerchantId)
        => _validator.Validate(new CreateKeetaIntegrationMerchantMappingCommand(1, 1, "internal-1", keetaMerchantId, "Store"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyStoreName_ShouldBeInvalid()
        => _validator.Validate(new CreateKeetaIntegrationMerchantMappingCommand(1, 1, "internal-1", 1, string.Empty))
            .IsValid.Should().BeFalse();
}
