using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByCompanyId
{
    public sealed record GetAllKeetaOrdersByCompanyIdQuery(
        long CompanyId) : IQuery<IReadOnlyList<KeetaIntegrationOrderResponse>>;
}
