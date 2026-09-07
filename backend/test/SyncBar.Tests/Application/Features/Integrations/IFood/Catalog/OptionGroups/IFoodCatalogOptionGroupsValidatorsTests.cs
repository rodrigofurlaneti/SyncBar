using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Catalog/OptionGroups — sem FluentValidation.TestHelper (não referenciado
// neste projeto), então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodCatalogOptionGroupsValidatorsTests
{
    [Fact]
    public void DeleteIfoodOptionCommandValidator_WithValidCommand_ShouldBeValid()
        => new DeleteIfoodOptionCommandValidator()
            .Validate(new DeleteIfoodOptionCommand(1, Guid.NewGuid(), Guid.NewGuid(), null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void DeleteIfoodOptionCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new DeleteIfoodOptionCommandValidator()
            .Validate(new DeleteIfoodOptionCommand(0, Guid.NewGuid(), Guid.NewGuid(), null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodOptionCommandValidator_WithEmptyOptionGroupId_ShouldBeInvalid()
        => new DeleteIfoodOptionCommandValidator()
            .Validate(new DeleteIfoodOptionCommand(1, Guid.Empty, Guid.NewGuid(), null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodOptionCommandValidator_WithEmptyProductId_ShouldBeInvalid()
        => new DeleteIfoodOptionCommandValidator()
            .Validate(new DeleteIfoodOptionCommand(1, Guid.NewGuid(), Guid.Empty, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodOptionGroupCommandValidator_WithValidCommand_ShouldBeValid()
        => new DeleteIfoodOptionGroupCommandValidator()
            .Validate(new DeleteIfoodOptionGroupCommand(1, Guid.NewGuid()))
            .IsValid.Should().BeTrue();

    [Fact]
    public void DeleteIfoodOptionGroupCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new DeleteIfoodOptionGroupCommandValidator()
            .Validate(new DeleteIfoodOptionGroupCommand(0, Guid.NewGuid()))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodOptionGroupCommandValidator_WithEmptyOptionGroupId_ShouldBeInvalid()
        => new DeleteIfoodOptionGroupCommandValidator()
            .Validate(new DeleteIfoodOptionGroupCommand(1, Guid.Empty))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DisassociateIfoodOptionGroupCommandValidator_WithValidCommand_ShouldBeValid()
        => new DisassociateIfoodOptionGroupCommandValidator()
            .Validate(new DisassociateIfoodOptionGroupCommand(1, Guid.NewGuid(), Guid.NewGuid()))
            .IsValid.Should().BeTrue();

    [Fact]
    public void DisassociateIfoodOptionGroupCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new DisassociateIfoodOptionGroupCommandValidator()
            .Validate(new DisassociateIfoodOptionGroupCommand(0, Guid.NewGuid(), Guid.NewGuid()))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DisassociateIfoodOptionGroupCommandValidator_WithEmptyOptionGroupId_ShouldBeInvalid()
        => new DisassociateIfoodOptionGroupCommandValidator()
            .Validate(new DisassociateIfoodOptionGroupCommand(1, Guid.Empty, Guid.NewGuid()))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DisassociateIfoodOptionGroupCommandValidator_WithEmptyProductId_ShouldBeInvalid()
        => new DisassociateIfoodOptionGroupCommandValidator()
            .Validate(new DisassociateIfoodOptionGroupCommand(1, Guid.NewGuid(), Guid.Empty))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodOptionExternalCodeCommandValidator_WithValidCommand_ShouldBeValid()
        => new SetIfoodOptionExternalCodeCommandValidator()
            .Validate(new SetIfoodOptionExternalCodeCommand(1, Guid.NewGuid(), "EXT-1", null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SetIfoodOptionExternalCodeCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new SetIfoodOptionExternalCodeCommandValidator()
            .Validate(new SetIfoodOptionExternalCodeCommand(0, Guid.NewGuid(), "EXT-1", null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodOptionExternalCodeCommandValidator_WithEmptyOptionId_ShouldBeInvalid()
        => new SetIfoodOptionExternalCodeCommandValidator()
            .Validate(new SetIfoodOptionExternalCodeCommand(1, Guid.Empty, "EXT-1", null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodOptionExternalCodeCommandValidator_WithEmptyExternalCode_ShouldBeInvalid()
        => new SetIfoodOptionExternalCodeCommandValidator()
            .Validate(new SetIfoodOptionExternalCodeCommand(1, Guid.NewGuid(), "", null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodOptionPriceCommandValidator_WithValidCommand_ShouldBeValid()
        => new SetIfoodOptionPriceCommandValidator()
            .Validate(new SetIfoodOptionPriceCommand(1, Guid.NewGuid(), 10m, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SetIfoodOptionPriceCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new SetIfoodOptionPriceCommandValidator()
            .Validate(new SetIfoodOptionPriceCommand(0, Guid.NewGuid(), 10m, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodOptionPriceCommandValidator_WithEmptyOptionId_ShouldBeInvalid()
        => new SetIfoodOptionPriceCommandValidator()
            .Validate(new SetIfoodOptionPriceCommand(1, Guid.Empty, 10m, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodOptionPriceCommandValidator_WithNegativeValue_ShouldBeInvalid()
        => new SetIfoodOptionPriceCommandValidator()
            .Validate(new SetIfoodOptionPriceCommand(1, Guid.NewGuid(), -1m, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodOptionStatusCommandValidator_WithValidCommand_ShouldBeValid()
        => new SetIfoodOptionStatusCommandValidator()
            .Validate(new SetIfoodOptionStatusCommand(1, Guid.NewGuid(), true, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SetIfoodOptionStatusCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new SetIfoodOptionStatusCommandValidator()
            .Validate(new SetIfoodOptionStatusCommand(0, Guid.NewGuid(), true, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodOptionStatusCommandValidator_WithEmptyOptionId_ShouldBeInvalid()
        => new SetIfoodOptionStatusCommandValidator()
            .Validate(new SetIfoodOptionStatusCommand(1, Guid.Empty, true, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpdateIfoodOptionGroupCommandValidator_WithValidCommand_ShouldBeValid()
        => new UpdateIfoodOptionGroupCommandValidator()
            .Validate(new UpdateIfoodOptionGroupCommand(1, Guid.NewGuid(), "Adicionais"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UpdateIfoodOptionGroupCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new UpdateIfoodOptionGroupCommandValidator()
            .Validate(new UpdateIfoodOptionGroupCommand(0, Guid.NewGuid(), "Adicionais"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpdateIfoodOptionGroupCommandValidator_WithEmptyOptionGroupId_ShouldBeInvalid()
        => new UpdateIfoodOptionGroupCommandValidator()
            .Validate(new UpdateIfoodOptionGroupCommand(1, Guid.Empty, "Adicionais"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpdateIfoodOptionGroupCommandValidator_WithEmptyName_ShouldBeInvalid()
        => new UpdateIfoodOptionGroupCommandValidator()
            .Validate(new UpdateIfoodOptionGroupCommand(1, Guid.NewGuid(), ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpdateIfoodOptionGroupCommandValidator_WithNameLongerThan100Chars_ShouldBeInvalid()
        => new UpdateIfoodOptionGroupCommandValidator()
            .Validate(new UpdateIfoodOptionGroupCommand(1, Guid.NewGuid(), new string('A', 101)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpdateIfoodOptionGroupStatusCommandValidator_WithValidCommand_ShouldBeValid()
        => new UpdateIfoodOptionGroupStatusCommandValidator()
            .Validate(new UpdateIfoodOptionGroupStatusCommand(1, Guid.NewGuid(), true))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UpdateIfoodOptionGroupStatusCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new UpdateIfoodOptionGroupStatusCommandValidator()
            .Validate(new UpdateIfoodOptionGroupStatusCommand(0, Guid.NewGuid(), true))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpdateIfoodOptionGroupStatusCommandValidator_WithEmptyOptionGroupId_ShouldBeInvalid()
        => new UpdateIfoodOptionGroupStatusCommandValidator()
            .Validate(new UpdateIfoodOptionGroupStatusCommand(1, Guid.Empty, true))
            .IsValid.Should().BeFalse();
}
