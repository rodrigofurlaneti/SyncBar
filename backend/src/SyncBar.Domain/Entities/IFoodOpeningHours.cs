using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Um registro por turno de funcionamento de uma filial no Ifood (Fase 5 — módulo Merchant). O
// SyncBar não tinha esse conceito modelado antes; esta é uma cópia local editável, sincronizada
// com o Ifood via PUT /opening-hours — que sempre SUBSTITUI a lista inteira de turnos, nunca
// atualiza incrementalmente. Por isso o handler de salvar sempre reenvia todos os turnos ativos
// da filial de uma vez (ver SaveIfoodOpeningHoursCommandHandler), nunca um turno isolado.
public sealed class IfoodOpeningHours : AggregateRoot
{
    public long BranchId { get; private set; }
    // 0 = domingo ... 6 = sábado, mesma convenção de DayOfWeek do .NET — evita reinventar enum.
    public int DayOfWeek { get; private set; }
    public TimeSpan Start { get; private set; }
    public int DurationMinutes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodOpeningHours() : base(0) { }

    private IfoodOpeningHours(long branchId, int dayOfWeek, TimeSpan start, int durationMinutes) : base(0)
    {
        BranchId = branchId;
        DayOfWeek = dayOfWeek;
        Start = start;
        DurationMinutes = durationMinutes;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodOpeningHours> Create(long branchId, int dayOfWeek, TimeSpan start, int durationMinutes)
    {
        if (dayOfWeek is < 0 or > 6)
            return Result.Failure<IfoodOpeningHours>(new Error("IfoodOpeningHours.InvalidDayOfWeek", "DayOfWeek must be between 0 (Sunday) and 6 (Saturday)."));
        if (durationMinutes <= 0)
            return Result.Failure<IfoodOpeningHours>(new Error("IfoodOpeningHours.InvalidDuration", "DurationMinutes must be greater than zero."));
        if (start < TimeSpan.Zero || start >= TimeSpan.FromDays(1))
            return Result.Failure<IfoodOpeningHours>(new Error("IfoodOpeningHours.InvalidStart", "Start must be a valid time of day."));

        return Result.Success(new IfoodOpeningHours(branchId, dayOfWeek, start, durationMinutes));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
