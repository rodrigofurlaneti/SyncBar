using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Um registro por turno de funcionamento de uma filial no iFood (Fase 5 — módulo Merchant). O
// SyncBar não tinha esse conceito modelado antes; esta é uma cópia local editável, sincronizada
// com o iFood via PUT /opening-hours — que sempre SUBSTITUI a lista inteira de turnos, nunca
// atualiza incrementalmente. Por isso o handler de salvar sempre reenvia todos os turnos ativos
// da filial de uma vez (ver SaveIFoodOpeningHoursCommandHandler), nunca um turno isolado.
public sealed class IFoodOpeningHours : AggregateRoot
{
    public long BranchId { get; private set; }
    // 0 = domingo ... 6 = sábado, mesma convenção de DayOfWeek do .NET — evita reinventar enum.
    public int DayOfWeek { get; private set; }
    public TimeSpan Start { get; private set; }
    public int DurationMinutes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodOpeningHours() : base(0) { }

    private IFoodOpeningHours(long branchId, int dayOfWeek, TimeSpan start, int durationMinutes) : base(0)
    {
        BranchId = branchId;
        DayOfWeek = dayOfWeek;
        Start = start;
        DurationMinutes = durationMinutes;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IFoodOpeningHours> Create(long branchId, int dayOfWeek, TimeSpan start, int durationMinutes)
    {
        if (dayOfWeek is < 0 or > 6)
            return Result.Failure<IFoodOpeningHours>(new Error("IFoodOpeningHours.InvalidDayOfWeek", "DayOfWeek must be between 0 (Sunday) and 6 (Saturday)."));
        if (durationMinutes <= 0)
            return Result.Failure<IFoodOpeningHours>(new Error("IFoodOpeningHours.InvalidDuration", "DurationMinutes must be greater than zero."));
        if (start < TimeSpan.Zero || start >= TimeSpan.FromDays(1))
            return Result.Failure<IFoodOpeningHours>(new Error("IFoodOpeningHours.InvalidStart", "Start must be a valid time of day."));

        return Result.Success(new IFoodOpeningHours(branchId, dayOfWeek, start, durationMinutes));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
