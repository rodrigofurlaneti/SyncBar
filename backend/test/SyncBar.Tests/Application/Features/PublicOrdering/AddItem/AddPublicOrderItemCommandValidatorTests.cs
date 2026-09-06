using FluentAssertions;
using SyncBar.Application.Features.PublicOrdering.AddItem;
using Xunit;

namespace SyncBar.Tests.Application.Features.PublicOrdering.AddItem;

public sealed class AddPublicOrderItemCommandValidatorTests
{
    private readonly AddPublicOrderItemCommandValidator _validator = new();

    private static AddPublicOrderItemCommand Valid() => new(Guid.NewGuid(), 1, 1, null);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyToken_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Token = Guid.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveProductId_ShouldBeInvalid(long productId)
        => _validator.Validate(Valid() with { ProductId = productId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveQuantity_ShouldBeInvalid(decimal quantity)
        => _validator.Validate(Valid() with { Quantity = quantity }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongNotes_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Notes = new string('a', 301) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongComandaCode_ShouldBeInvalid()
        => _validator.Validate(Valid() with { ComandaCode = new string('a', 51) }).IsValid.Should().BeFalse();
}
