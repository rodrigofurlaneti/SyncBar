using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Catalog.Products;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Catalog/Products — sem FluentValidation.TestHelper (não referenciado neste
// projeto), então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodCatalogProductsValidatorsTests
{
    private static readonly IfoodBatchProductPriceInput ValidPriceItem = new("prod-1", null, 10m, null, null);
    private static readonly IfoodBatchProductStatusInput ValidStatusItem = new("prod-1", null, "AVAILABLE", null);

    [Fact]
    public void BatchUpdateIfoodProductPricesCommandValidator_WithValidCommand_ShouldBeValid()
        => new BatchUpdateIfoodProductPricesCommandValidator()
            .Validate(new BatchUpdateIfoodProductPricesCommand(1, [ValidPriceItem], null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void BatchUpdateIfoodProductPricesCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new BatchUpdateIfoodProductPricesCommandValidator()
            .Validate(new BatchUpdateIfoodProductPricesCommand(0, [ValidPriceItem], null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void BatchUpdateIfoodProductPricesCommandValidator_WithEmptyItems_ShouldBeInvalid()
        => new BatchUpdateIfoodProductPricesCommandValidator()
            .Validate(new BatchUpdateIfoodProductPricesCommand(1, [], null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void BatchUpdateIfoodProductPricesCommandValidator_WithNegativeItemValue_ShouldBeInvalid()
        => new BatchUpdateIfoodProductPricesCommandValidator()
            .Validate(new BatchUpdateIfoodProductPricesCommand(1, [new IfoodBatchProductPriceInput("prod-1", null, -1m, null, null)], null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void BatchUpdateIfoodProductStatusesCommandValidator_WithValidCommand_ShouldBeValid()
        => new BatchUpdateIfoodProductStatusesCommandValidator()
            .Validate(new BatchUpdateIfoodProductStatusesCommand(1, [ValidStatusItem], null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void BatchUpdateIfoodProductStatusesCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new BatchUpdateIfoodProductStatusesCommandValidator()
            .Validate(new BatchUpdateIfoodProductStatusesCommand(0, [ValidStatusItem], null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void BatchUpdateIfoodProductStatusesCommandValidator_WithEmptyItems_ShouldBeInvalid()
        => new BatchUpdateIfoodProductStatusesCommandValidator()
            .Validate(new BatchUpdateIfoodProductStatusesCommand(1, [], null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodProductCommandValidator_WithValidCommand_ShouldBeValid()
        => new CreateIfoodProductCommandValidator()
            .Validate(new CreateIfoodProductCommand(1, null, "Burger", null, null, null, null, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void CreateIfoodProductCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new CreateIfoodProductCommandValidator()
            .Validate(new CreateIfoodProductCommand(0, null, "Burger", null, null, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodProductCommandValidator_WithEmptyName_ShouldBeInvalid()
        => new CreateIfoodProductCommandValidator()
            .Validate(new CreateIfoodProductCommand(1, null, "", null, null, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodProductCommandValidator_WithNameLongerThan100Chars_ShouldBeInvalid()
        => new CreateIfoodProductCommandValidator()
            .Validate(new CreateIfoodProductCommand(1, null, new string('A', 101), null, null, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodProductCommandValidator_WithValidCommand_ShouldBeValid()
        => new DeleteIfoodProductCommandValidator()
            .Validate(new DeleteIfoodProductCommand(1, Guid.NewGuid()))
            .IsValid.Should().BeTrue();

    [Fact]
    public void DeleteIfoodProductCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new DeleteIfoodProductCommandValidator()
            .Validate(new DeleteIfoodProductCommand(0, Guid.NewGuid()))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodProductCommandValidator_WithEmptyProductId_ShouldBeInvalid()
        => new DeleteIfoodProductCommandValidator()
            .Validate(new DeleteIfoodProductCommand(1, Guid.Empty))
            .IsValid.Should().BeFalse();

    [Fact]
    public void EditIfoodProductCommandValidator_WithValidCommand_ShouldBeValid()
        => new EditIfoodProductCommandValidator()
            .Validate(new EditIfoodProductCommand(1, Guid.NewGuid(), "Burger", null, null, null, null, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void EditIfoodProductCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new EditIfoodProductCommandValidator()
            .Validate(new EditIfoodProductCommand(0, Guid.NewGuid(), "Burger", null, null, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void EditIfoodProductCommandValidator_WithEmptyProductId_ShouldBeInvalid()
        => new EditIfoodProductCommandValidator()
            .Validate(new EditIfoodProductCommand(1, Guid.Empty, "Burger", null, null, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void EditIfoodProductCommandValidator_WithEmptyName_ShouldBeInvalid()
        => new EditIfoodProductCommandValidator()
            .Validate(new EditIfoodProductCommand(1, Guid.NewGuid(), "", null, null, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void EditIfoodProductCommandValidator_WithNameLongerThan100Chars_ShouldBeInvalid()
        => new EditIfoodProductCommandValidator()
            .Validate(new EditIfoodProductCommand(1, Guid.NewGuid(), new string('A', 101), null, null, null, null, null, null))
            .IsValid.Should().BeFalse();
}
