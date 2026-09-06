using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForBranch
{
    public sealed record ExistsKeetaSettingForBranchQuery(
        long BranchId) : IQuery<bool>;
}
