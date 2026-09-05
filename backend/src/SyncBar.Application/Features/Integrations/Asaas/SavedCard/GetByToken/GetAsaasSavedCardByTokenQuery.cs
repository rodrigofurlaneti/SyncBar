using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByToken
{
    public sealed record GetAsaasSavedCardByTokenQuery(
        string CreditCardToken) : IQuery<AsaasIntegrationSavedCardResponse>;
}
