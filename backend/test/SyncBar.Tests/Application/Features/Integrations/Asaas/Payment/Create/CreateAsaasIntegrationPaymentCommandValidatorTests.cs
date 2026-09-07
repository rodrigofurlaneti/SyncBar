using System;
using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.Create;

public sealed class CreateAsaasIntegrationPaymentCommandValidatorTests
{
    private readonly CreateAsaasIntegrationPaymentCommandValidator _validator = new();

    private static CreditCardDataRequest ValidCreditCard()
        => new("John Doe", "4111111111111111", "12", "2030", "123");

    private static CreateAsaasIntegrationPaymentCommand ValidPixCommand()
        => new(1, 1, 1, "PIX", 100m, DateTime.Today.AddDays(1));

    [Fact]
    public void Validate_ValidPixCommand_ShouldBeValid()
        => _validator.Validate(ValidPixCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCreditCardCommandWithFullCardData_ShouldBeValid()
        => _validator.Validate(ValidPixCommand() with { BillingType = "CREDIT_CARD", CreditCard = ValidCreditCard() })
            .IsValid.Should().BeTrue();

    [Fact]
    public void Validate_CreditCardCommandWithTokenInsteadOfCardData_ShouldBeValid()
        => _validator.Validate(ValidPixCommand() with { BillingType = "CREDIT_CARD", CreditCardToken = "tok_000001" })
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(ValidPixCommand() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerOrderId_ShouldBeInvalid(long customerOrderId)
        => _validator.Validate(ValidPixCommand() with { CustomerOrderId = customerOrderId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveValue_ShouldBeInvalid(decimal value)
        => _validator.Validate(ValidPixCommand() with { Value = value }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_DueDateBeforeToday_ShouldBeInvalid()
        => _validator.Validate(ValidPixCommand() with { DueDate = DateTime.Today.AddDays(-1) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyBillingType_ShouldBeInvalid()
        => _validator.Validate(ValidPixCommand() with { BillingType = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_UnrecognizedBillingType_ShouldBeInvalid()
        => _validator.Validate(ValidPixCommand() with { BillingType = "INVALID" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_InstallmentCountBelowOne_ShouldBeInvalid()
        => _validator.Validate(ValidPixCommand() with { InstallmentCount = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_CreditCardBillingTypeWithoutTokenOrCardData_ShouldBeInvalid()
        => _validator.Validate(ValidPixCommand() with { BillingType = "CREDIT_CARD" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_CreditCardWithEmptyHolderName_ShouldBeInvalid()
        => _validator.Validate(ValidPixCommand() with
        {
            BillingType = "CREDIT_CARD",
            CreditCard = ValidCreditCard() with { HolderName = string.Empty }
        }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_CreditCardWithInvalidNumber_ShouldBeInvalid()
        => _validator.Validate(ValidPixCommand() with
        {
            BillingType = "CREDIT_CARD",
            CreditCard = ValidCreditCard() with { Number = "4111111111111112" }
        }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("1")]
    [InlineData("123")]
    public void Validate_CreditCardWithInvalidExpiryMonthLength_ShouldBeInvalid(string expiryMonth)
        => _validator.Validate(ValidPixCommand() with
        {
            BillingType = "CREDIT_CARD",
            CreditCard = ValidCreditCard() with { ExpiryMonth = expiryMonth }
        }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("203")]
    [InlineData("20300")]
    public void Validate_CreditCardWithInvalidExpiryYearLength_ShouldBeInvalid(string expiryYear)
        => _validator.Validate(ValidPixCommand() with
        {
            BillingType = "CREDIT_CARD",
            CreditCard = ValidCreditCard() with { ExpiryYear = expiryYear }
        }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    public void Validate_CreditCardWithInvalidCcvLength_ShouldBeInvalid(string ccv)
        => _validator.Validate(ValidPixCommand() with
        {
            BillingType = "CREDIT_CARD",
            CreditCard = ValidCreditCard() with { Ccv = ccv }
        }).IsValid.Should().BeFalse();
}
