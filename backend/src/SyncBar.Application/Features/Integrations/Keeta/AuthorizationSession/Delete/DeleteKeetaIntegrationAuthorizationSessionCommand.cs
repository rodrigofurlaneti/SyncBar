using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Delete
{
    public sealed record DeleteKeetaIntegrationAuthorizationSessionCommand(
        long Id,
        long CompanyId) : ICommand;
}
