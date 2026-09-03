using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fechamento Diário / Turno Comercial: consolida, totaliza e audita todas as
// CashSession (e, por extensão, as CashMovement que já compõem o ExpectedAmount
// de cada uma) de uma filial num período. Uma filial só pode ter um turno
// Aberto por vez (garantido pelo handler via GetOpenByBranchAsync, mesmo
// padrão de "uma sessão aberta por caixa" do CashSession).
public sealed class ShiftClosing : AggregateRoot
{
    public long BranchId { get; private set; }
    public long ShiftClosingStatusId { get; private set; }
    public long OpenedByEmployeeId { get; private set; }
    public long? ClosedByEmployeeId { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime? PeriodEnd { get; private set; }
    public int CashSessionsCount { get; private set; }
    public decimal TotalOpeningAmount { get; private set; }
    public decimal TotalExpectedAmount { get; private set; }
    public decimal TotalRealizedAmount { get; private set; }
    public decimal TotalDifferenceAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ShiftClosing() : base(0) { }

    private ShiftClosing(long branchId, long openedByEmployeeId) : base(0)
    {
        BranchId = branchId;
        OpenedByEmployeeId = openedByEmployeeId;
        PeriodStart = DateTime.Now;
        ShiftClosingStatusId = ShiftClosingStatusIds.Aberto;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<ShiftClosing> Open(long branchId, long openedByEmployeeId)
    {
        if (branchId <= 0)
            return Result.Failure<ShiftClosing>(new Error("ShiftClosing.InvalidBranch", "Branch is required."));

        if (openedByEmployeeId <= 0)
            return Result.Failure<ShiftClosing>(new Error("ShiftClosing.InvalidEmployee", "Employee is required."));

        return Result.Success(new ShiftClosing(branchId, openedByEmployeeId));
    }

    public bool IsOpen() => ShiftClosingStatusId == ShiftClosingStatusIds.Aberto;

    // Consolidação automática: recebe todas as CashSession da filial no período
    // (já buscadas pelo handler via ICashSessionRepository.GetByBranchAndPeriodAsync)
    // e totaliza fundo de troco, esperado, realizado e diferença geral. Bloqueia o
    // fechamento do turno se ainda houver caixa aberto pendente no período.
    public Result Close(
        long closedByEmployeeId,
        DateTime periodEnd,
        IReadOnlyCollection<CashSession> cashSessions,
        string? notes)
    {
        if (!IsOpen())
            return Result.Failure(new Error("ShiftClosing.NotOpen", "Only an open shift can be closed."));

        if (periodEnd < PeriodStart)
            return Result.Failure(new Error("ShiftClosing.InvalidPeriod", "Period end must not be earlier than period start."));

        if (cashSessions.Any(s => s.IsActive && s.IsOpen()))
            return Result.Failure(new Error(
                "ShiftClosing.OpenCashSessionsPending",
                "There are cash registers still open for this branch in the period; close them before closing the shift."));

        var activeSessions = cashSessions.Where(s => s.IsActive).ToList();

        CashSessionsCount = activeSessions.Count;
        TotalOpeningAmount = activeSessions.Sum(s => s.OpeningAmount);
        TotalExpectedAmount = activeSessions.Sum(s => s.ExpectedAmount ?? 0);
        TotalRealizedAmount = activeSessions.Sum(s => s.ClosingAmount ?? 0);
        TotalDifferenceAmount = TotalRealizedAmount - TotalExpectedAmount;

        ClosedByEmployeeId = closedByEmployeeId;
        PeriodEnd = periodEnd;
        Notes = notes;
        ShiftClosingStatusId = ShiftClosingStatusIds.Fechado;
        UpdatedAt = DateTime.Now;

        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
