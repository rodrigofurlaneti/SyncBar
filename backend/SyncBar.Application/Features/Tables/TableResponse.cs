namespace SyncBar.Application.Features.Tables;

public sealed record TableResponse(
    long Id,
    long BranchId,
    long TableStatusId,
    int Number,
    int? Capacity);
