using FluentAssertions;
using SyncBar.Application.Features.Checkout.PayOrderWithBoleto;
using SyncBar.Application.Features.Checkout.PayOrderWithCreditCard;
using SyncBar.Application.Features.Checkout.PayOrderWithPix;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Checkout;

public sealed class CheckoutValidatorsTests
{
    private static CreditCardDataRequest ValidCard() => new("Fabio Cardoso", "4111111111111111", "12", "2030", "123");

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void PayOrderWithPixCommandValidator_ShouldValidateCustomerOrderId(long customerOrderId, bool expected)
        => new PayOrderWithPixCommandValidator().Validate(new PayOrderWithPixCommand(customerOrderId)).IsValid.Should().Be(expected);

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void PayOrderWithBoletoCommandValidator_ShouldValidateCustomerOrderId(long customerOrderId, bool expected)
        => new PayOrderWithBoletoCommandValidator().Validate(new PayOrderWithBoletoCommand(customerOrderId)).IsValid.Should().Be(expected);

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_ValidSavedCardOnly_ShouldBeValid()
    {
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: 5, Card: null);

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_ValidNewCard_ShouldBeValid()
    {
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: null, Card: ValidCard());

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_NeitherSavedCardNorCard_ShouldBeInvalid()
    {
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: null, Card: null);

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_BothSavedCardAndCard_ShouldBeInvalid()
    {
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: 5, Card: ValidCard());

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_InvalidCustomerOrderId_ShouldBeInvalid()
    {
        var command = new PayOrderWithCreditCardCommand(0, SavedCardId: 5, Card: null);

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_EmptyHolderName_ShouldBeInvalid()
    {
        var card = ValidCard() with { HolderName = "" };
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: null, Card: card);

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_InvalidCardNumber_ShouldBeInvalid()
    {
        var card = ValidCard() with { Number = "not-a-card" };
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: null, Card: card);

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_InvalidExpiryMonth_ShouldBeInvalid()
    {
        var card = ValidCard() with { ExpiryMonth = "1" };
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: null, Card: card);

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_InvalidExpiryYear_ShouldBeInvalid()
    {
        var card = ValidCard() with { ExpiryYear = "30" };
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: null, Card: card);

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PayOrderWithCreditCardCommandValidator_InvalidCcv_ShouldBeInvalid()
    {
        var card = ValidCard() with { Ccv = "12" };
        var command = new PayOrderWithCreditCardCommand(1, SavedCardId: null, Card: card);

        new PayOrderWithCreditCardCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }
}
