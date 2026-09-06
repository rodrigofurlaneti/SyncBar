using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchId
{
    public sealed record GetKeetaSettingByBranchIdQuery(
        long BranchId) : IQuery<KeetaIntegrationSettingResponse>;
}
