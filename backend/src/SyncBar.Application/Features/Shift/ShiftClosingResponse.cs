namespace SyncBar.Application.Features.Shift;

public sealed record ShiftClosingResponse(
    long Id,
    long BranchId,
    long ShiftClosingStatusId,
    long OpenedByEmployeeId,
    long? ClosedByEmployeeId,
    DateTime PeriodStart,
    DateTime? PeriodEnd,
    int CashSessionsCount,
    decimal TotalOpeningAmount,
    decimal TotalExpectedAmount,
    decimal TotalRealizedAmount,
    decimal TotalDifferenceAmount,
    string? Notes);
