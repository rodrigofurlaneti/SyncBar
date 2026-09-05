using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Update
{
    public sealed record UpdateAsaasIntegrationPaymentCommand(
        long Id,
        string Status,
        decimal? NetValue = null,
        DateTime? PaymentDate = null,
        string? PixQrCodeBase64 = null,
        string? PixPayload = null,
        string? InvoiceUrl = null,
        string? BankSlipUrl = null) : ICommand;
}
