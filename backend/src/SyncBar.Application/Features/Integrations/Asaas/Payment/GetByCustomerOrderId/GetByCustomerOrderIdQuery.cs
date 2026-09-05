using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId
{
    public sealed record GetByCustomerOrderIdQuery(
        long CustomerOrderId) : IQuery<AsaasIntegrationPaymentResponse>;
}
