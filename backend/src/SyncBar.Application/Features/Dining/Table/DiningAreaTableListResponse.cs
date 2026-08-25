namespace SyncBar.Application.Features.Dining.Table
{
    public sealed record DiningAreaTableListResponse(
        long Id,
        long DiningTableId,
        bool IsActive);
}
