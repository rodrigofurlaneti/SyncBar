using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetPendingSessions
{
    public sealed record GetPendingKeetaAuthorizationSessionsQuery
        : IQuery<IReadOnlyList<KeetaIntegrationAuthorizationSessionResponse>>;
}
