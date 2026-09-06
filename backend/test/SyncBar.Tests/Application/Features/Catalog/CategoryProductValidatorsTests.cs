using FluentAssertions;
using SyncBar.Application.Features.Catalog.CreateCategory;
using SyncBar.Application.Features.Catalog.CreateProduct;
using SyncBar.Application.Features.Catalog.UpdateCategory;
using SyncBar.Application.Features.Catalog.UpdateProduct;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog;

public sealed class CategoryProductValidatorsTests
{
    private static CreateProductCommand ValidCreateProduct() =>
        new(1, 1, 1, "X-Burguer", "Descrição", "789123", 25m, 10m, false, 10);

    private static UpdateProductCommand ValidUpdateProduct() =>
        new(1, 1, 1, "X-Burguer", "Descrição", "789123", 25m, 10m, false, 10);

    [Fact]
    public void CreateCategoryCommandValidator_ValidCommand_ShouldBeValid()
        => new CreateCategoryCommandValidator().Validate(new CreateCategoryCommand(1, "Bebidas", 0)).IsValid.Should().BeTrue();

    [Fact]
    public void CreateCategoryCommandValidator_InvalidCompanyId_ShouldBeInvalid()
        => new CreateCategoryCommandValidator().Validate(new CreateCategoryCommand(0, "Bebidas", 0)).IsValid.Should().BeFalse();

    [Fact]
    public void CreateCategoryCommandValidator_EmptyName_ShouldBeInvalid()
        => new CreateCategoryCommandValidator().Validate(new CreateCategoryCommand(1, "", 0)).IsValid.Should().BeFalse();

    [Fact]
    public void CreateCategoryCommandValidator_NameTooLong_ShouldBeInvalid()
        => new CreateCategoryCommandValidator().Validate(new CreateCategoryCommand(1, new string('a', 101), 0)).IsValid.Should().BeFalse();

    [Fact]
    public void CreateCategoryCommandValidator_NegativeDisplayOrder_ShouldBeInvalid()
        => new CreateCategoryCommandValidator().Validate(new CreateCategoryCommand(1, "Bebidas", -1)).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateCategoryCommandValidator_ValidCommand_ShouldBeValid()
        => new UpdateCategoryCommandValidator().Validate(new UpdateCategoryCommand(1, "Bebidas", 0)).IsValid.Should().BeTrue();

    [Fact]
    public void UpdateCategoryCommandValidator_InvalidCategoryId_ShouldBeInvalid()
        => new UpdateCategoryCommandValidator().Validate(new UpdateCategoryCommand(0, "Bebidas", 0)).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateCategoryCommandValidator_EmptyName_ShouldBeInvalid()
        => new UpdateCategoryCommandValidator().Validate(new UpdateCategoryCommand(1, "", 0)).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateCategoryCommandValidator_NegativeDisplayOrder_ShouldBeInvalid()
        => new UpdateCategoryCommandValidator().Validate(new UpdateCategoryCommand(1, "Bebidas", -1)).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_ValidCommand_ShouldBeValid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct()).IsValid.Should().BeTrue();

    [Fact]
    public void CreateProductCommandValidator_InvalidCompanyId_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { CompanyId = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_InvalidCategoryId_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { CategoryId = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_InvalidUnitOfMeasureId_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { UnitOfMeasureId = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_EmptyName_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { Name = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_NameTooLong_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { Name = new string('a', 151) }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_DescriptionTooLong_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { Description = new string('a', 501) }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_BarcodeTooLong_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { Barcode = new string('1', 51) }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_NegativeSalePrice_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { SalePrice = -1m }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_NegativeCostPrice_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { CostPrice = -1m }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_NullCostPrice_ShouldBeValid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { CostPrice = null }).IsValid.Should().BeTrue();

    [Fact]
    public void CreateProductCommandValidator_ZeroPreparationTime_ShouldBeInvalid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { PreparationTimeMinutes = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateProductCommandValidator_NullPreparationTime_ShouldBeValid()
        => new CreateProductCommandValidator().Validate(ValidCreateProduct() with { PreparationTimeMinutes = null }).IsValid.Should().BeTrue();

    [Fact]
    public void UpdateProductCommandValidator_ValidCommand_ShouldBeValid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct()).IsValid.Should().BeTrue();

    [Fact]
    public void UpdateProductCommandValidator_InvalidProductId_ShouldBeInvalid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct() with { ProductId = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateProductCommandValidator_InvalidCategoryId_ShouldBeInvalid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct() with { CategoryId = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateProductCommandValidator_InvalidUnitOfMeasureId_ShouldBeInvalid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct() with { UnitOfMeasureId = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateProductCommandValidator_EmptyName_ShouldBeInvalid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct() with { Name = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateProductCommandValidator_DescriptionTooLong_ShouldBeInvalid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct() with { Description = new string('a', 501) }).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateProductCommandValidator_BarcodeTooLong_ShouldBeInvalid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct() with { Barcode = new string('1', 51) }).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateProductCommandValidator_NegativeSalePrice_ShouldBeInvalid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct() with { SalePrice = -1m }).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateProductCommandValidator_NegativeCostPrice_ShouldBeInvalid()
        => new UpdateProductCommandValidator().Validate(ValidUpdateProduct() with { CostPrice = -1m }).IsValid.Should().BeFalse();
}
