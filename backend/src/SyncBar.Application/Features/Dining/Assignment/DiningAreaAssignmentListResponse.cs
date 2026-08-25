namespace SyncBar.Application.Features.Dining.Assignment
{
    public sealed record DiningAreaAssignmentListResponse(
        long Id,
        long DiningAreaId,
        long EmployeeId,
        DateTime StartAt);
}
