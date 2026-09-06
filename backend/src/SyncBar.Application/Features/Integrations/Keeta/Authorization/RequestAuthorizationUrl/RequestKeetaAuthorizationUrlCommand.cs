using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl
{
    public sealed record RequestKeetaAuthorizationUrlCommand(
        long CompanyId,
        long BranchId,
        string RedirectUri) : ICommand<RequestKeetaAuthorizationUrlResponse>;
}
