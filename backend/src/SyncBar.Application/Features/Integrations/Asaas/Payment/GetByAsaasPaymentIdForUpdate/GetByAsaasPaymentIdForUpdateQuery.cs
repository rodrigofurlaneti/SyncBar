using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentIdForUpdate
{
    public sealed record GetByAsaasPaymentIdForUpdateQuery(
        string AsaasPaymentId) : IQuery<AsaasIntegrationPaymentResponse>;
}
