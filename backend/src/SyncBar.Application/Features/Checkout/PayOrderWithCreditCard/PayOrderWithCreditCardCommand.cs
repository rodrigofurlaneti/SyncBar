using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;

namespace SyncBar.Application.Features.Checkout.PayOrderWithCreditCard
{
    public sealed record PayOrderWithCreditCardCommand(
        long CustomerOrderId,
        long? SavedCardId,
        CreditCardDataRequest? Card,
        bool SaveCard = false) : ICommand<PayOrderWithCreditCardResponse>;

    public sealed record PayOrderWithCreditCardResponse(
        long PaymentId,
        string AsaasPaymentId,
        string Status,
        string? CardBrand,
        string? Last4Digits,
        decimal Value);
}
