using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Catalog.Categories;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Catalog/Categories — sem FluentValidation.TestHelper (não referenciado neste
// projeto), então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodCatalogCategoriesValidatorsTests
{
    [Fact]
    public void CreateIfoodCategoryCommandValidator_WithValidCommand_ShouldBeValid()
        => new CreateIfoodCategoryCommandValidator()
            .Validate(new CreateIfoodCategoryCommand(1, "catalog-1", "Bebidas"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void CreateIfoodCategoryCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new CreateIfoodCategoryCommandValidator()
            .Validate(new CreateIfoodCategoryCommand(0, "catalog-1", "Bebidas"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodCategoryCommandValidator_WithEmptyCatalogId_ShouldBeInvalid()
        => new CreateIfoodCategoryCommandValidator()
            .Validate(new CreateIfoodCategoryCommand(1, "", "Bebidas"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodCategoryCommandValidator_WithEmptyName_ShouldBeInvalid()
        => new CreateIfoodCategoryCommandValidator()
            .Validate(new CreateIfoodCategoryCommand(1, "catalog-1", ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodCategoryCommandValidator_WithNameLongerThan100Chars_ShouldBeInvalid()
        => new CreateIfoodCategoryCommandValidator()
            .Validate(new CreateIfoodCategoryCommand(1, "catalog-1", new string('A', 101)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodCategoryCommandValidator_WithValidCommand_ShouldBeValid()
        => new DeleteIfoodCategoryCommandValidator()
            .Validate(new DeleteIfoodCategoryCommand(1, "category-1"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void DeleteIfoodCategoryCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new DeleteIfoodCategoryCommandValidator()
            .Validate(new DeleteIfoodCategoryCommand(0, "category-1"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodCategoryCommandValidator_WithEmptyCategoryId_ShouldBeInvalid()
        => new DeleteIfoodCategoryCommandValidator()
            .Validate(new DeleteIfoodCategoryCommand(1, ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void EditIfoodCategoryCommandValidator_WithValidCommand_ShouldBeValid()
        => new EditIfoodCategoryCommandValidator()
            .Validate(new EditIfoodCategoryCommand(1, "catalog-1", "category-1", "Bebidas", null, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void EditIfoodCategoryCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new EditIfoodCategoryCommandValidator()
            .Validate(new EditIfoodCategoryCommand(0, "catalog-1", "category-1", "Bebidas", null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void EditIfoodCategoryCommandValidator_WithEmptyCatalogId_ShouldBeInvalid()
        => new EditIfoodCategoryCommandValidator()
            .Validate(new EditIfoodCategoryCommand(1, "", "category-1", "Bebidas", null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void EditIfoodCategoryCommandValidator_WithEmptyCategoryId_ShouldBeInvalid()
        => new EditIfoodCategoryCommandValidator()
            .Validate(new EditIfoodCategoryCommand(1, "catalog-1", "", "Bebidas", null, null, null))
            .IsValid.Should().BeFalse();
}
