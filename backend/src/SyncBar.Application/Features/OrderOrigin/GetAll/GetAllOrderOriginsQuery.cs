using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.OrderOrigin.GetAll
{
    public sealed record GetAllOrderOriginsQuery() : IQuery<IReadOnlyCollection<OrderOriginResponse>>;
}
