using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId
{
    public sealed record ExistsByAsaasPaymentIdQuery(
        string AsaasPaymentId) : IQuery<bool>;
}
