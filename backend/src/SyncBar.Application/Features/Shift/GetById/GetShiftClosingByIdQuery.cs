using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Shift.GetById;

public sealed record GetShiftClosingByIdQuery(long ShiftClosingId) : IQuery<ShiftClosingResponse>;
