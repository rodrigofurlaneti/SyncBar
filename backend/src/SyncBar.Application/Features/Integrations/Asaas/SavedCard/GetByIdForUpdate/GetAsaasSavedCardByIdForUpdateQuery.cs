using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByIdForUpdate
{
    public sealed record GetAsaasSavedCardByIdForUpdateQuery(
        long Id) : IQuery<AsaasIntegrationSavedCardResponse>;
}
