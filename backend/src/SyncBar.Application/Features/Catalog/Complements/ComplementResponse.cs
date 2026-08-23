namespace SyncBar.Application.Features.Catalog.Complements;

// Fase 18 (combos) — LinkedProductId/LinkedProductImageUrl só vêm preenchidos quando o
// ComplementItem por trás desta opção aponta pra um Product real (ver comentário em
// ComplementItem) — o front-end usa a imagem do produto em vez de mostrar só o nome quando presentes.
public sealed record ComplementResponse(
    long Id,
    long ComplementItemId,
    string ComplementItemName,
    decimal ExtraPrice,
    bool IsActive,
    long? LinkedProductId = null,
    string? LinkedProductImageUrl = null);
