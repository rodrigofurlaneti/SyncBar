using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;

// ⚠️ RISCO CONHECIDO: assim como sinalizado em IIfoodCatalogClient.UploadImageAsync, a doc oficial
// não documenta o schema do corpo/resposta deste endpoint (Postman mostra literalmente "<object>"
// pros dois, sem exemplo de campo algum) — mesma ressalva já registrada pro fluxo de tempo de
// preparo (ver SetIfoodPreparationTimeCommand/Handler no módulo Merchant) pra endpoints sem
// exemplo real colado na doc no momento da implementação. O SyncBar aceita o JSON pronto que o
// chamador mandar (JsonBody) e repassa cru pro Ifood; a resposta também é devolvida crua
// (RawPayload). Tratar como não confiável até testar contra o sandbox.
public sealed record IfoodImageUploadResponse(string? RawPayload);

public sealed record UploadIfoodImageCommand(long BranchId, string JsonBody) : ICommand<IfoodImageUploadResponse>;
