using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Area.GetById
{
    public sealed record GetDiningAreaByIdQuery(long Id) : IQuery<DiningAreaResponse>;
}
