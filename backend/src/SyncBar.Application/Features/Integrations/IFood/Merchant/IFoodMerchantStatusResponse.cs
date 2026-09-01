namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record IfoodMerchantValidationResponse(string Id, string State, string? Message);

// Available adicionado na Fase 13 — ver comentário em IfoodMerchantStatusResult sobre por que o
// client passou a extrair esse campo (antes descartado) da resposta do Ifood.
public sealed record IfoodMerchantStatusResponse(
    string? OperationState, bool Available, IReadOnlyCollection<IfoodMerchantValidationResponse> Validations);
