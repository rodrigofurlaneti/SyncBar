using FluentAssertions;
using SyncBar.Application.Features.Integrations.WhatsApp.SendImage;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.WhatsApp.SendImage;

public sealed class SendWhatsAppImageCommandValidatorTests
{
    private readonly SendWhatsAppImageCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new SendWhatsAppImageCommand("5511999999999", "Ola", "https://cdn.example.com/img.jpg"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyPhoneNumber_ShouldBeInvalid()
        => _validator.Validate(new SendWhatsAppImageCommand("", "Ola", "https://cdn.example.com/img.jpg"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_PhoneNumberLongerThanTwentyChars_ShouldBeInvalid()
        => _validator.Validate(new SendWhatsAppImageCommand(new string('9', 21), "Ola", "https://cdn.example.com/img.jpg"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyMessage_ShouldBeInvalid()
        => _validator.Validate(new SendWhatsAppImageCommand("5511999999999", "", "https://cdn.example.com/img.jpg"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_MessageLongerThanThousandChars_ShouldBeInvalid()
        => _validator.Validate(new SendWhatsAppImageCommand("5511999999999", new string('a', 1001), "https://cdn.example.com/img.jpg"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyFileUrl_ShouldBeInvalid()
        => _validator.Validate(new SendWhatsAppImageCommand("5511999999999", "Ola", ""))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/img.jpg")]
    public void Validate_InvalidFileUrl_ShouldBeInvalid(string fileUrl)
        => _validator.Validate(new SendWhatsAppImageCommand("5511999999999", "Ola", fileUrl))
            .IsValid.Should().BeFalse();
}
