using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Catalog.Items;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Catalog/Items — sem FluentValidation.TestHelper (não referenciado neste
// projeto), então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodCatalogItemsValidatorsTests
{
    [Fact]
    public void DeleteIfoodItemCommandValidator_WithValidCommand_ShouldBeValid()
        => new DeleteIfoodItemCommandValidator()
            .Validate(new DeleteIfoodItemCommand(1, "category-1", Guid.NewGuid(), null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void DeleteIfoodItemCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new DeleteIfoodItemCommandValidator()
            .Validate(new DeleteIfoodItemCommand(0, "category-1", Guid.NewGuid(), null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodItemCommandValidator_WithEmptyCategoryId_ShouldBeInvalid()
        => new DeleteIfoodItemCommandValidator()
            .Validate(new DeleteIfoodItemCommand(1, "", Guid.NewGuid(), null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodItemCommandValidator_WithEmptyProductId_ShouldBeInvalid()
        => new DeleteIfoodItemCommandValidator()
            .Validate(new DeleteIfoodItemCommand(1, "category-1", Guid.Empty, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodItemExternalCodeCommandValidator_WithValidCommand_ShouldBeValid()
        => new SetIfoodItemExternalCodeCommandValidator()
            .Validate(new SetIfoodItemExternalCodeCommand(1, Guid.NewGuid(), "EXT-1", null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SetIfoodItemExternalCodeCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new SetIfoodItemExternalCodeCommandValidator()
            .Validate(new SetIfoodItemExternalCodeCommand(0, Guid.NewGuid(), "EXT-1", null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodItemExternalCodeCommandValidator_WithEmptyItemId_ShouldBeInvalid()
        => new SetIfoodItemExternalCodeCommandValidator()
            .Validate(new SetIfoodItemExternalCodeCommand(1, Guid.Empty, "EXT-1", null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodItemPriceCommandValidator_WithValidCommand_ShouldBeValid()
        => new SetIfoodItemPriceCommandValidator()
            .Validate(new SetIfoodItemPriceCommand(1, Guid.NewGuid(), 10m, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SetIfoodItemPriceCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new SetIfoodItemPriceCommandValidator()
            .Validate(new SetIfoodItemPriceCommand(0, Guid.NewGuid(), 10m, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodItemPriceCommandValidator_WithEmptyItemId_ShouldBeInvalid()
        => new SetIfoodItemPriceCommandValidator()
            .Validate(new SetIfoodItemPriceCommand(1, Guid.Empty, 10m, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetIfoodItemPriceCommandValidator_WithNegativeValue_ShouldBeInvalid()
        => new SetIfoodItemPriceCommandValidator()
            .Validate(new SetIfoodItemPriceCommand(1, Guid.NewGuid(), -1m, null, null))
            .IsValid.Should().BeFalse();
}
