namespace SyncBar.Application.Features.Catalog.Complements;

public sealed record ComplementResponse(long Id, long ComplementItemId, string ComplementItemName, decimal ExtraPrice, bool IsActive);
