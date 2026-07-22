using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Promotion : AggregateRoot
{
    public long BranchId { get; private set; }
    public long ProductId { get; private set; }
    public string Name { get; private set; } = null!;
    public int DayOfWeek { get; private set; }          // 0=Domingo ... 6=Sabado (System.DayOfWeek)
    public int StartMinuteOfDay { get; private set; }   // 960 = 16:00
    public int EndMinuteOfDay { get; private set; }     // 1200 = 20:00
    public long PromotionTypeId { get; private set; }   // 1 = EmDobro | 2 = Desconto
    public decimal? DiscountRate { get; private set; }  // 0.25 = 25% (apenas Desconto)
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Promotion() : base(0) { }

    private Promotion(long branchId, long productId, string name, int dayOfWeek,
        int startMinuteOfDay, int endMinuteOfDay, long promotionTypeId, decimal? discountRate) : base(0)
    {
        BranchId = branchId;
        ProductId = productId;
        Name = name;
        DayOfWeek = dayOfWeek;
        StartMinuteOfDay = startMinuteOfDay;
        EndMinuteOfDay = endMinuteOfDay;
        PromotionTypeId = promotionTypeId;
        DiscountRate = discountRate;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Promotion> Create(long branchId, long productId, string name,
        int dayOfWeek, int startMinuteOfDay, int endMinuteOfDay,
        long promotionTypeId = PromotionTypeIds.EmDobro, decimal? discountRate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Promotion>(new Error("Promotion.EmptyName", "Name is required."));
        if (dayOfWeek is < 0 or > 6)
            return Result.Failure<Promotion>(new Error("Promotion.InvalidDay", "Day of week must be between 0 (Sunday) and 6 (Saturday)."));
        if (startMinuteOfDay is < 0 or > 1439 || endMinuteOfDay is < 1 or > 1440)
            return Result.Failure<Promotion>(new Error("Promotion.InvalidWindow", "Minutes must be within the day."));
        if (startMinuteOfDay >= endMinuteOfDay)
            return Result.Failure<Promotion>(new Error("Promotion.InvalidWindow", "Start must be before end."));
        if (promotionTypeId is not (PromotionTypeIds.EmDobro or PromotionTypeIds.Desconto))
            return Result.Failure<Promotion>(new Error("Promotion.InvalidType", "Promotion type must be EmDobro or Desconto."));
        if (promotionTypeId == PromotionTypeIds.Desconto && discountRate is not (> 0 and < 1))
            return Result.Failure<Promotion>(new Error("Promotion.InvalidDiscount", "Discount rate must be between 0 and 1."));

        return Result.Success(new Promotion(branchId, productId, name, dayOfWeek,
            startMinuteOfDay, endMinuteOfDay, promotionTypeId,
            promotionTypeId == PromotionTypeIds.Desconto ? discountRate : null));
    }

    // O cliente precisa PEDIR dentro da janela — avaliado no horario local do bar.
    public bool IsActiveAt(DateTime localNow)
    {
        if (!IsActive) return false;
        if ((int)localNow.DayOfWeek != DayOfWeek) return false;
        var minuteOfDay = localNow.Hour * 60 + localNow.Minute;
        return minuteOfDay >= StartMinuteOfDay && minuteOfDay < EndMinuteOfDay;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
