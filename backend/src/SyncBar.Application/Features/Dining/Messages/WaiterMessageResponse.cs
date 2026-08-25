namespace SyncBar.Application.Features.Dining.Messages
{
    public sealed record WaiterMessageResponse(
        long Id,
        long BranchId,
        long SenderEmployeeId,
        long? RecipientEmployeeId,
        long? DiningAreaId,
        string Message,
        bool IsRead,
        string CreatedAt
    );
}
