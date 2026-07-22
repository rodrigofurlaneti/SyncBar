using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Promotions.Create;

public sealed record CreatePromotionCommand(
    long BranchId,
    long ProductId,
    string Name,
    int DayOfWeek,
    int StartMinuteOfDay,
    int EndMinuteOfDay,
    long PromotionTypeId,
    decimal? DiscountRate) : ICommand<long>;
