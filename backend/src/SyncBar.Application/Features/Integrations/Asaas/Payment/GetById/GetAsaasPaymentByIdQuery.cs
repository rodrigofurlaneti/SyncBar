using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetById
{
    public sealed record GetAsaasPaymentByIdQuery(
        long Id) : IQuery<AsaasIntegrationPaymentResponse>;
}
