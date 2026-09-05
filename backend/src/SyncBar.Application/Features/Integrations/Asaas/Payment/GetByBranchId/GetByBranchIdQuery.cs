using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByBranchId
{
    public sealed record GetByBranchIdQuery(
        long BranchId) : IQuery<IReadOnlyList<AsaasIntegrationPaymentResponse>>;
}
