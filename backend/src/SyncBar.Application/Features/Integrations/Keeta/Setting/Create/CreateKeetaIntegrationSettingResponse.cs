namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Create
{
    public sealed record CreateKeetaIntegrationSettingResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string BaseUrl);
}
