using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForBranch
{
    public sealed record ExistsAsaasSettingForBranchQuery(
        long CompanyId,
        long BranchId) : IQuery<bool>;
}
