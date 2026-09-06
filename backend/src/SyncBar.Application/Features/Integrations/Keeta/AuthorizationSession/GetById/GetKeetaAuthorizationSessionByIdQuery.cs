using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById
{
    public sealed record GetKeetaAuthorizationSessionByIdQuery(
        long Id) : IQuery<KeetaIntegrationAuthorizationSessionResponse>;
}
