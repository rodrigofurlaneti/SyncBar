using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Update
{
    public sealed record UpdateKeetaIntegrationAuthorizationSessionCommand(
        long Id,
        long CompanyId,
        long? KeetaMerchantId = null,
        string? AuthorizationCode = null,
        string? State = null,
        bool MarkAsProcessed = false) : ICommand;
}
