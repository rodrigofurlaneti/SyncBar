using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Shift.CloseShift;

public sealed record CloseShiftClosingCommand(
    long ShiftClosingId,
    long ClosedByEmployeeId,
    string? Notes) : ICommand<ShiftClosingResponse>;
