namespace SyncBar.Application.Features.PublicOrdering.GetPublicBill
{
    public sealed record PublicBillItemResponse(
        long ItemId,
        string ProductName,
        decimal Quantity,
        decimal UnitPrice,
        decimal TotalPrice,
        long StatusId,
        DateTime RequestedAt,
        string? Notes
    )
    {
        public IReadOnlyCollection<PublicItemCustomizationResponse> OptionalExtras { get; init; } = [];
        public IReadOnlyCollection<PublicItemCustomizationResponse> Boosts { get; init; } = [];
    }
}
