namespace SyncBar.Application.Features.Dining.Table
{
    public sealed record DiningAreaTableResponse(
        long Id,
        long DiningAreaId,
        long DiningTableId,
        bool IsActive);
}
