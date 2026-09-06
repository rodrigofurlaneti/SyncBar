using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByCompanyIdForUpdate
{
    public sealed record GetKeetaSettingByCompanyIdForUpdateQuery(
        long CompanyId) : IQuery<KeetaIntegrationSettingResponse>;
}
