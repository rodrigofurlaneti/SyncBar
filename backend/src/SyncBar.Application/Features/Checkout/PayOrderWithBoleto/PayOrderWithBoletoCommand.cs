using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Checkout.PayOrderWithBoleto
{
    public sealed record PayOrderWithBoletoCommand(long CustomerOrderId) : ICommand<PayOrderWithBoletoResponse>;

    public sealed record PayOrderWithBoletoResponse(
        long PaymentId,
        string AsaasPaymentId,
        string Status,
        string? BankSlipUrl,
        string? IdentificationField,
        string? BarCode,
        decimal Value,
        DateTime DueDate);
}
