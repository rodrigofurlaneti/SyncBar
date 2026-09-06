using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetByAuthId
{
    public sealed record GetKeetaAuthorizationSessionByAuthIdQuery(
        string AuthId) : IQuery<KeetaIntegrationAuthorizationSessionResponse>;
}
