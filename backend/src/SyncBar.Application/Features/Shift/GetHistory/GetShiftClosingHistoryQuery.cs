using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Shift.GetHistory;

public sealed record GetShiftClosingHistoryQuery(
    long BranchId,
    int ReferenceYear,
    int ReferenceMonth) : IQuery<IReadOnlyCollection<ShiftClosingResponse>>;
