using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Create
{
    public sealed record CreateAsaasIntegrationPaymentResponse(
        long PaymentId,
        string AsaasPaymentId,
        string Status,
        string? PixQrCodeBase64,
        string? PixPayload,
        string? InvoiceUrl,
        string? BankSlipUrl);
}
