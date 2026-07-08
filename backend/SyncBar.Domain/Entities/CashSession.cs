using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CashSession : AggregateRoot
{
    public long CashRegisterId { get; private set; }
    public long CashSessionStatusId { get; private set; }
    public long OpenedByEmployeeId { get; private set; }
    public long? ClosedByEmployeeId { get; private set; }
    public decimal OpeningAmount { get; private set; }
    public decimal? ClosingAmount { get; private set; }
    public decimal? ExpectedAmount { get; private set; }
    public decimal? DifferenceAmount { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CashSession() : base(0) { }

    private CashSession(long cashRegisterId, long openedByEmployeeId, decimal openingAmount) : base(0)
    {
        CashRegisterId = cashRegisterId;
        OpenedByEmployeeId = openedByEmployeeId;
        OpeningAmount = openingAmount;
        CashSessionStatusId = CashSessionStatusIds.Aberto;
        OpenedAt = DateTime.UtcNow;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CashSession> Open(long cashRegisterId, long openedByEmployeeId, decimal openingAmount)
    {
        if (openingAmount < 0)
            return Result.Failure<CashSession>(new Error("CashSession.InvalidOpeningAmount", "Opening amount cannot be negative."));

        return Result.Success(new CashSession(cashRegisterId, openedByEmployeeId, openingAmount));
    }

    public Result Close(long closedByEmployeeId, decimal closingAmount, decimal expectedAmount)
    {
        if (CashSessionStatusId != CashSessionStatusIds.Aberto)
            return Result.Failure(new Error("CashSession.NotOpen", "Only an open session can be closed."));
        if (closingAmount < 0)
            return Result.Failure(new Error("CashSession.InvalidClosingAmount", "Closing amount cannot be negative."));

        ClosedByEmployeeId = closedByEmployeeId;
        ClosingAmount = closingAmount;
        ExpectedAmount = expectedAmount;
        DifferenceAmount = closingAmount - expectedAmount;
        CashSessionStatusId = CashSessionStatusIds.Fechado;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public bool IsOpen() => CashSessionStatusId == CashSessionStatusIds.Aberto;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
