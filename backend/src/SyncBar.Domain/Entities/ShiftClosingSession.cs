using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Vinculo de agregacao entre o fechamento de turno e cada CashSession
// consolidada nele — trilha de auditoria imutavel de "quais caixas entraram
// neste fechamento". Os valores (esperado/realizado/diferenca) permanecem na
// propria CashSession, fonte unica da verdade; aqui so a associacao.
public sealed class ShiftClosingSession : Entity
{
    public long ShiftClosingId { get; private set; }
    public long CashSessionId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ShiftClosingSession() : base(0) { }

    private ShiftClosingSession(long shiftClosingId, long cashSessionId) : base(0)
    {
        ShiftClosingId = shiftClosingId;
        CashSessionId = cashSessionId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<ShiftClosingSession> Create(long shiftClosingId, long cashSessionId)
    {
        if (shiftClosingId <= 0)
            return Result.Failure<ShiftClosingSession>(new Error("ShiftClosingSession.InvalidShiftClosing", "ShiftClosing is required."));

        if (cashSessionId <= 0)
            return Result.Failure<ShiftClosingSession>(new Error("ShiftClosingSession.InvalidCashSession", "CashSession is required."));

        return Result.Success(new ShiftClosingSession(shiftClosingId, cashSessionId));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
