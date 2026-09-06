using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.RefreshAccessToken
{
    public sealed record RefreshKeetaAccessTokenCommand(long CompanyId, long BranchId) : ICommand<RefreshKeetaAccessTokenResponse>;
}
