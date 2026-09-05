using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetById
{
    public sealed record GetAsaasSavedCardByIdQuery(
        long Id) : IQuery<AsaasIntegrationSavedCardResponse>;
}
