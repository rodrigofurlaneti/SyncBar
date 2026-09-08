using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Shift.GetOpenShift;

public sealed record GetOpenShiftClosingQuery(long BranchId) : IQuery<ShiftClosingResponse>;
