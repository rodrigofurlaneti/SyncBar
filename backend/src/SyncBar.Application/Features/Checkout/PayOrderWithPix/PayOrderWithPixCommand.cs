using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Checkout.PayOrderWithPix
{
    public sealed record PayOrderWithPixCommand(long CustomerOrderId) : ICommand<PayOrderWithPixResponse>;

    public sealed record PayOrderWithPixResponse(
        long PaymentId,
        string AsaasPaymentId,
        string Status,
        string? PixQrCodeBase64,
        string? PixPayload,
        string? InvoiceUrl,
        decimal Value);
}
