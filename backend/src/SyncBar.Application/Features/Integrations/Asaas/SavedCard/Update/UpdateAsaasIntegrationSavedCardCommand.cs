using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Update
{
    public sealed record UpdateAsaasIntegrationSavedCardCommand(
        long Id,
        long CustomerId,
        long CompanyId,
        string? HolderName = null,
        string? ExpiryMonth = null,
        string? ExpiryYear = null,
        bool? SetAsDefault = null) : ICommand;
}
