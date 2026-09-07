using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.Update;

public sealed class UpdateAsaasIntegrationSavedCardCommandValidatorTests
{
    private readonly UpdateAsaasIntegrationSavedCardCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommandWithoutOptionalFields_ShouldBeValid()
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithAllOptionalFields_ShouldBeValid()
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1, "John Doe", "12", "2099"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(id, 1, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(1, customerId, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(1, 1, companyId)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HolderNameExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1, HolderName: new string('a', 101)))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData("13")]
    [InlineData("00")]
    [InlineData("1")]
    public void Validate_InvalidExpiryMonthFormat_ShouldBeInvalid(string expiryMonth)
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1, ExpiryMonth: expiryMonth))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData("99")]
    [InlineData("20999")]
    [InlineData("abcd")]
    public void Validate_InvalidExpiryYearFormat_ShouldBeInvalid(string expiryYear)
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1, ExpiryYear: expiryYear))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ExpiryYearInThePast_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1, ExpiryYear: "2000"))
            .IsValid.Should().BeFalse();
}
