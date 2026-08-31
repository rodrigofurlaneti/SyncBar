namespace SyncBar.Application.Features.PublicOrdering.GetPublicBill
{
    public sealed record PublicBillResponse(
        long OrderId,
        string TableNumber,
        string Status,
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal ServiceFeeAmount,
        decimal TotalAmount,
        IReadOnlyCollection<PublicBillItemResponse> Items
    );
}
