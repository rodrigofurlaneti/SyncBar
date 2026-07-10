using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Preparation.GetQueue;

public sealed record PreparationItemResponse(
    long OrderItemId,
    long ProductId,
    string ProductName,
    decimal Quantity,
    long OrderItemStatusId,
    string? Notes,
    DateTime StartedAt,
    int LimitMinutes,
    bool IsBarItem);

public sealed record PreparationTicketResponse(
    long CustomerOrderId,
    int? TableNumber,
    string? ComandaCode,
    DateTime OpenedAt,
    IReadOnlyCollection<PreparationItemResponse> Items);

public sealed record GetPreparationQueueQuery(long BranchId) : IQuery<IReadOnlyCollection<PreparationTicketResponse>>;
