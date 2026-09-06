using FluentAssertions;
using SyncBar.Application.Features.Catalog.SetProductImage;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.SetProductImage;

public sealed class SetProductImageCommandValidatorTests
{
    private static byte[] ValidContent() => new byte[1024];

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => new SetProductImageCommandValidator().Validate(new SetProductImageCommand(1, ".png", ValidContent())).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_InvalidProductId_ShouldBeInvalid()
        => new SetProductImageCommandValidator().Validate(new SetProductImageCommand(0, ".png", ValidContent())).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    [InlineData(".JPG")]
    public void Validate_AllowedExtensions_ShouldBeValid(string extension)
        => new SetProductImageCommandValidator().Validate(new SetProductImageCommand(1, extension, ValidContent())).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    [InlineData("")]
    public void Validate_DisallowedExtension_ShouldBeInvalid(string extension)
        => new SetProductImageCommandValidator().Validate(new SetProductImageCommand(1, extension, ValidContent())).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyContent_ShouldBeInvalid()
        => new SetProductImageCommandValidator().Validate(new SetProductImageCommand(1, ".png", [])).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ContentTooLarge_ShouldBeInvalid()
        => new SetProductImageCommandValidator().Validate(new SetProductImageCommand(1, ".png", new byte[2 * 1024 * 1024 + 1])).IsValid.Should().BeFalse();
}
