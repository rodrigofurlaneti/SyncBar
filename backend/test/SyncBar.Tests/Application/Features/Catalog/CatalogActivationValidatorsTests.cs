using FluentAssertions;
using SyncBar.Application.Features.Catalog.ActivateCategory;
using SyncBar.Application.Features.Catalog.ActivateProduct;
using SyncBar.Application.Features.Catalog.DeactivateCategory;
using SyncBar.Application.Features.Catalog.DeactivateProduct;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog;

public sealed class CatalogActivationValidatorsTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void ActivateCategoryCommandValidator_ShouldValidateCategoryId(long categoryId, bool expected)
        => new ActivateCategoryCommandValidator().Validate(new ActivateCategoryCommand(categoryId)).IsValid.Should().Be(expected);

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void ActivateProductCommandValidator_ShouldValidateProductId(long productId, bool expected)
        => new ActivateProductCommandValidator().Validate(new ActivateProductCommand(productId)).IsValid.Should().Be(expected);

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void DeactivateCategoryCommandValidator_ShouldValidateCategoryId(long categoryId, bool expected)
        => new DeactivateCategoryCommandValidator().Validate(new DeactivateCategoryCommand(categoryId)).IsValid.Should().Be(expected);

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void DeactivateProductCommandValidator_ShouldValidateProductId(long productId, bool expected)
        => new DeactivateProductCommandValidator().Validate(new DeactivateProductCommand(productId)).IsValid.Should().Be(expected);
}
