using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetById
{
    public sealed record GetKeetaSettingByIdQuery(
        long Id) : IQuery<KeetaIntegrationSettingResponse>;
}
