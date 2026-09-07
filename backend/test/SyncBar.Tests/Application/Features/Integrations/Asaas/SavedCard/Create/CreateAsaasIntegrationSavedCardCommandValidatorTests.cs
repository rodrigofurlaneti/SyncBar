using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.Create;

public sealed class CreateAsaasIntegrationSavedCardCommandValidatorTests
{
    private readonly CreateAsaasIntegrationSavedCardCommandValidator _validator = new();

    private static CreateAsaasIntegrationSavedCardCommand ValidCommand()
        => new(1, 1, "John Doe", "4111111111111111", "12", "2099", "123");

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(ValidCommand() with { CustomerId = customerId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(ValidCommand() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyHolderName_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { HolderName = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HolderNameExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { HolderName = new string('a', 101) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyCardNumber_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { CardNumber = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_InvalidCardNumber_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { CardNumber = "4111111111111112" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyExpiryMonth_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { ExpiryMonth = string.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("13")]
    [InlineData("00")]
    [InlineData("1")]
    public void Validate_InvalidExpiryMonthFormat_ShouldBeInvalid(string expiryMonth)
        => _validator.Validate(ValidCommand() with { ExpiryMonth = expiryMonth }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyExpiryYear_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { ExpiryYear = string.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("99")]
    [InlineData("20999")]
    [InlineData("abcd")]
    public void Validate_InvalidExpiryYearFormat_ShouldBeInvalid(string expiryYear)
        => _validator.Validate(ValidCommand() with { ExpiryYear = expiryYear }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ExpiryYearInThePast_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { ExpiryYear = "2000" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyCcv_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { Ccv = string.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("abc")]
    public void Validate_InvalidCcvFormat_ShouldBeInvalid(string ccv)
        => _validator.Validate(ValidCommand() with { Ccv = ccv }).IsValid.Should().BeFalse();
}
