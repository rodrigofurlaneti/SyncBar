using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByCompanyId
{
    public sealed record GetKeetaSettingByCompanyIdQuery(
        long CompanyId) : IQuery<KeetaIntegrationSettingResponse>;
}
