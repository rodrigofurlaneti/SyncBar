namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback
{
    public sealed record HandleKeetaOAuthCallbackResponse(
        long SessionId,
        string AuthId,
        bool Processed,
        IReadOnlyList<long> MappedKeetaMerchantIds);
}
