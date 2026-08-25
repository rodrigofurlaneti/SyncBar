using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Area.GetByBranchId
{
    public sealed record GetDiningAreasByBranchQuery(long BranchId) : IQuery<IReadOnlyCollection<DiningAreaListResponse>>;
}
