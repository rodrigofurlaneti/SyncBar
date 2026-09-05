namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive
{
    public sealed record AsaasIntegrationSettingResponse(
        long Id,
        long CompanyId,
        long? BranchId,
        string Environment,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
}
