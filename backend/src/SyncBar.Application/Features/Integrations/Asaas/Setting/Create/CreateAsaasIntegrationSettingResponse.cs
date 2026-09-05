namespace SyncBar.Application.Features.Integrations.Asaas.Setting.Create
{
    public sealed record CreateAsaasIntegrationSettingResponse(
        long Id,
        long CompanyId,
        long? BranchId,
        string Environment,
        bool IsActive);
}
