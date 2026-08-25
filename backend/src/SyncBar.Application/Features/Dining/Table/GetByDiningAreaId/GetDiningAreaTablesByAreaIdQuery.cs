using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Table.GetByDiningAreaId
{
    public sealed record GetDiningAreaTablesByAreaIdQuery(long DiningAreaId) : IQuery<IReadOnlyCollection<DiningAreaTableListResponse>>;
}
