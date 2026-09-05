using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByIdForUpdate
{
    public sealed record GetAsaasPaymentByIdForUpdateQuery(
        long Id) : IQuery<AsaasIntegrationPaymentResponse>;
}
