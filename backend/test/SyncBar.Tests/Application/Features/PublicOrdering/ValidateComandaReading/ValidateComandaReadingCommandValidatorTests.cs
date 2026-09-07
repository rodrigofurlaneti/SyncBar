using FluentAssertions;
using SyncBar.Application.Features.PublicOrdering.ValidateComandaReading;
using Xunit;

namespace SyncBar.Tests.Application.Features.PublicOrdering.ValidateComandaReading;

public sealed class ValidateComandaReadingCommandValidatorTests
{
    private readonly ValidateComandaReadingCommandValidator _validator = new();

    private static ValidateComandaReadingCommand ValidCamera() => new(
        Guid.NewGuid(),
        "COMANDA1",
        "camera",
        null,
        "base64photo");

    private static ValidateComandaReadingCommand ValidBarcode() => new(
        Guid.NewGuid(),
        "COMANDA1",
        "barcode",
        "scanned-value",
        null);

    [Fact]
    public void Validate_ValidCameraCommand_ShouldBeValid()
        => _validator.Validate(ValidCamera()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidBarcodeCommand_ShouldBeValid()
        => _validator.Validate(ValidBarcode()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyTableToken_ShouldBeInvalid()
        => _validator.Validate(ValidCamera() with { TableToken = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyComandaCode_ShouldBeInvalid()
        => _validator.Validate(ValidCamera() with { ComandaCode = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyMethod_ShouldBeInvalid()
        => _validator.Validate(ValidCamera() with { Method = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_InvalidMethod_ShouldBeInvalid()
        => _validator.Validate(ValidCamera() with { Method = "invalid" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_CameraMethodWithoutPhotoBase64_ShouldBeInvalid()
        => _validator.Validate(ValidCamera() with { PhotoBase64 = null }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_BarcodeMethodWithoutScannedValue_ShouldBeInvalid()
        => _validator.Validate(ValidBarcode() with { ScannedValue = null }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_QrcodeMethodWithoutScannedValue_ShouldBeInvalid()
        => _validator.Validate(ValidBarcode() with { Method = "qrcode", ScannedValue = null }).IsValid.Should().BeFalse();
}
