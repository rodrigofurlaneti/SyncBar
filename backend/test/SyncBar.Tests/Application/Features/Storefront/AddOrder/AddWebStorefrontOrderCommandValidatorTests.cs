using FluentAssertions;
using SyncBar.Application.Features.Storefront.AddOrder;
using Xunit;

namespace SyncBar.Tests.Application.Features.Storefront.AddOrder;

public sealed class AddWebStorefrontOrderCommandValidatorTests
{
    private readonly AddWebStorefrontOrderCommandValidator _validator = new();

    private static WebStorefrontItemDto ValidItem() => new(1, 1, null, null);

    private static AddWebStorefrontOrderCommand Valid() => new(
        1,
        null,
        "John Doe",
        null,
        null,
        [ValidItem()]);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(Valid() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(Valid() with { CustomerId = customerId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyCustomerName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { CustomerName = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongCustomerName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { CustomerName = new string('a', 151) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongCustomerPhone_ShouldBeInvalid()
        => _validator.Validate(Valid() with { CustomerPhone = new string('1', 21) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyItems_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Items = [] }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ItemWithNonPositiveProductId_ShouldBeInvalid(long productId)
        => _validator.Validate(Valid() with { Items = [ValidItem() with { ProductId = productId }] }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ItemWithNonPositiveQuantity_ShouldBeInvalid(decimal quantity)
        => _validator.Validate(Valid() with { Items = [ValidItem() with { Quantity = quantity }] }).IsValid.Should().BeFalse();
}
