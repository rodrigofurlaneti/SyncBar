namespace SyncBar.Application.Features.Catalog.Complements;

public sealed record ComplementGroupResponse(
    long Id,
    string Name,
    long ComplementGroupTypeId,
    int MinSelection,
    int MaxSelection,
    bool IsActive,
    IReadOnlyCollection<ComplementResponse> Complements);
