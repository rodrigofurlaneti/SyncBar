using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

// ⚠️ RISCO CONHECIDO: assim como sinalizado em IIFoodCatalogClient.UploadImageAsync, a doc oficial
// não documenta o schema do corpo/resposta deste endpoint (Postman mostra literalmente "<object>"
// pros dois, sem exemplo de campo algum) — mesma ressalva já registrada pro fluxo de tempo de
// preparo (ver SetIFoodPreparationTimeCommand/Handler no módulo Merchant) pra endpoints sem
// exemplo real colado na doc no momento da implementação. O SyncBar aceita o JSON pronto que o
// chamador mandar (JsonBody) e repassa cru pro iFood; a resposta também é devolvida crua
// (RawPayload). Tratar como não confiável até testar contra o sandbox.
public sealed record IFoodImageUploadResponse(string? RawPayload);

public sealed record UploadIFoodImageCommand(long BranchId, string JsonBody) : ICommand<IFoodImageUploadResponse>;
