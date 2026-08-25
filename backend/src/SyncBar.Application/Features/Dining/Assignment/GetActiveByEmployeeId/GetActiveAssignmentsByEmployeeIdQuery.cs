using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Assignment.GetActiveByEmployeeId
{
    public sealed record GetActiveAssignmentsByEmployeeIdQuery(long EmployeeId) : IQuery<IReadOnlyCollection<DiningAreaAssignmentListResponse>>;
}
