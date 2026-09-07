using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.OrderOrigin.GetAll;
namespace SyncBar.Application.Features.OrderOrigin.GetById
{
    public sealed record GetOrderOriginByIdQuery(long Id) : IQuery<OrderOriginResponse>;
}
