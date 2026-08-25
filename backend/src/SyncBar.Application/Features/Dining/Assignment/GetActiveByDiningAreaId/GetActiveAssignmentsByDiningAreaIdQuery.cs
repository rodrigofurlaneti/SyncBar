using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Assignment.GetActiveByDiningAreaId
{
    public sealed record GetActiveAssignmentsByDiningAreaIdQuery(long DiningAreaId) : IQuery<IReadOnlyCollection<DiningAreaAssignmentListResponse>>;
}
