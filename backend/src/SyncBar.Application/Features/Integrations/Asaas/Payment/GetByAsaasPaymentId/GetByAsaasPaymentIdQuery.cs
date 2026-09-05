using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId
{
    public sealed record GetByAsaasPaymentIdQuery(
        string AsaasPaymentId) : IQuery<AsaasIntegrationPaymentResponse>;
}
