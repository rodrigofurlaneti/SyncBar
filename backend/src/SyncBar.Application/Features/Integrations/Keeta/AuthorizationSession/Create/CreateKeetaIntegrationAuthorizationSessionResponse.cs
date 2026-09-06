namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Create
{
    public sealed record CreateKeetaIntegrationAuthorizationSessionResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string AuthId,
        int OperationType);
}
