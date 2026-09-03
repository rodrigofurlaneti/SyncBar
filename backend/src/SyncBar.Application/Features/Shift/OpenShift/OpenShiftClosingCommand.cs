using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Shift.OpenShift;

public sealed record OpenShiftClosingCommand(
    long BranchId,
    long OpenedByEmployeeId) : ICommand<long>;
