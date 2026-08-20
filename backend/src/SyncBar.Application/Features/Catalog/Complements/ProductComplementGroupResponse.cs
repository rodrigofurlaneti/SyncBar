namespace SyncBar.Application.Features.Catalog.Complements;

public sealed record ProductComplementGroupResponse(
    long ProductComplementGroupId,
    long ComplementGroupId,
    string ComplementGroupName,
    long ComplementGroupTypeId,
    int MinSelection,
    int MaxSelection,
    int DisplayOrder,
    IReadOnlyCollection<ComplementResponse> Complements);
