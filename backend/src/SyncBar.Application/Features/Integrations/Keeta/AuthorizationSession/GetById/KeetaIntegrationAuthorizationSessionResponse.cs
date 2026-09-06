namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById
{
    public sealed record KeetaIntegrationAuthorizationSessionResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string AuthId,
        string? State,
        long? KeetaMerchantId,
        string? AuthorizationCode,
        int OperationType,
        bool IsProcessed,
        DateTime CreatedAtUtc);
}
