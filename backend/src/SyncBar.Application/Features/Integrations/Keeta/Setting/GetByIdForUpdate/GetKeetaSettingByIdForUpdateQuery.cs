using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByIdForUpdate
{
    public sealed record GetKeetaSettingByIdForUpdateQuery(
        long Id) : IQuery<KeetaIntegrationSettingResponse>;
}
