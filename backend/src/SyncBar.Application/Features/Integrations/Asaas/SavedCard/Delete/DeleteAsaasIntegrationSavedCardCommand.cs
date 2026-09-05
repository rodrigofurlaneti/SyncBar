using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Delete
{
    public sealed record DeleteAsaasIntegrationSavedCardCommand(
        long Id,
        long CustomerId,
        long CompanyId) : ICommand;
}
