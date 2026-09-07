namespace SyncBar.Application.Features.OrderOrigin.GetAll
{
    public sealed record OrderOriginResponse(
        long Id,
        long? CompanyId,
        long? BranchId,
        string Name,
        bool IsActive,
        DateTime CreatedAt);
}
