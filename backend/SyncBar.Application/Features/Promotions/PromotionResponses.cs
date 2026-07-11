namespace SyncBar.Application.Features.Promotions;

public sealed record PromotionResponse(
    long Id,
    long ProductId,
    string Name,
    int DayOfWeek,
    int StartMinuteOfDay,
    int EndMinuteOfDay,
    long PromotionTypeId,
    decimal? DiscountRate);

public sealed record ActivePromotionResponse(
    long ProductId,
    string Name,
    int EndMinuteOfDay,
    long PromotionTypeId,
    decimal? DiscountRate);
