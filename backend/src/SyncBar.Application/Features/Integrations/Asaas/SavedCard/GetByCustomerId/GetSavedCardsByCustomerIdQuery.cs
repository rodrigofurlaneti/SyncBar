using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId
{
    public sealed record GetSavedCardsByCustomerIdQuery(
        long CustomerId) : IQuery<IReadOnlyList<AsaasIntegrationSavedCardResponse>>;
}
