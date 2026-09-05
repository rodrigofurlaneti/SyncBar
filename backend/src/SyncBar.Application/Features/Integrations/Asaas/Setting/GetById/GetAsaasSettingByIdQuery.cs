using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetById
{
    public sealed record GetAsaasSettingByIdQuery(
        long Id) : IQuery<AsaasIntegrationSettingResponse>;
}
