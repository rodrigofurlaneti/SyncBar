using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood;

// Cobertura das regras de validação (FluentValidation) dos comandos raiz de
// Integrations/Ifood (settings, mapeamento de merchant e teste de conexão) — sem
// FluentValidation.TestHelper (não referenciado neste projeto), então os asserts usam
// ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodSettingsValidatorsTests
{
    [Fact]
    public void SaveIfoodSettingsCommandValidator_WithValidCommand_ShouldBeValid()
        => new SaveIfoodSettingsCommandValidator()
            .Validate(new SaveIfoodSettingsCommand(1, "client-id", "client-secret", false, "customer-1"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SaveIfoodSettingsCommandValidator_WithZeroCompanyId_ShouldBeInvalid()
        => new SaveIfoodSettingsCommandValidator()
            .Validate(new SaveIfoodSettingsCommand(0, "client-id", "client-secret", false, "customer-1"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SaveIfoodSettingsCommandValidator_WithClientIdLongerThan200Chars_ShouldBeInvalid()
        => new SaveIfoodSettingsCommandValidator()
            .Validate(new SaveIfoodSettingsCommand(1, new string('A', 201), "client-secret", false, "customer-1"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SaveIfoodSettingsCommandValidator_WithEnabledAndEmptyClientId_ShouldBeInvalid()
        => new SaveIfoodSettingsCommandValidator()
            .Validate(new SaveIfoodSettingsCommand(1, null, "client-secret", true, "customer-1"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SaveIfoodSettingsCommandValidator_WithDisabledAndEmptyClientId_ShouldBeValid()
        => new SaveIfoodSettingsCommandValidator()
            .Validate(new SaveIfoodSettingsCommand(1, null, null, false, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SaveIfoodSettingsCommandValidator_WithIfoodCustomerIdLongerThan100Chars_ShouldBeInvalid()
        => new SaveIfoodSettingsCommandValidator()
            .Validate(new SaveIfoodSettingsCommand(1, "client-id", "client-secret", false, new string('A', 101)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodMerchantMappingCommandValidator_WithValidCommand_ShouldBeValid()
        => new SetIfoodMerchantMappingCommandValidator()
            .Validate(new SetIfoodMerchantMappingCommand(1, "merchant-1", "uuid-1"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SetIfoodMerchantMappingCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new SetIfoodMerchantMappingCommandValidator()
            .Validate(new SetIfoodMerchantMappingCommand(0, "merchant-1", "uuid-1"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodMerchantMappingCommandValidator_WithMerchantIdLongerThan100Chars_ShouldBeInvalid()
        => new SetIfoodMerchantMappingCommandValidator()
            .Validate(new SetIfoodMerchantMappingCommand(1, new string('A', 101), "uuid-1"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodMerchantMappingCommandValidator_WithMerchantUuidLongerThan100Chars_ShouldBeInvalid()
        => new SetIfoodMerchantMappingCommandValidator()
            .Validate(new SetIfoodMerchantMappingCommand(1, "merchant-1", new string('A', 101)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodMerchantMappingCommandValidator_WithNullMerchantIdAndUuid_ShouldBeValid()
        => new SetIfoodMerchantMappingCommandValidator()
            .Validate(new SetIfoodMerchantMappingCommand(1, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void TestIfoodConnectionCommandValidator_WithValidCommand_ShouldBeValid()
        => new TestIfoodConnectionCommandValidator().Validate(new TestIfoodConnectionCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void TestIfoodConnectionCommandValidator_WithZeroCompanyId_ShouldBeInvalid()
        => new TestIfoodConnectionCommandValidator().Validate(new TestIfoodConnectionCommand(0)).IsValid.Should().BeFalse();
}
