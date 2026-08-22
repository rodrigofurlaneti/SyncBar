namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record IFoodMerchantValidationResponse(string Id, string State, string? Message);

// Available adicionado na Fase 13 — ver comentário em IFoodMerchantStatusResult sobre por que o
// client passou a extrair esse campo (antes descartado) da resposta do iFood.
public sealed record IFoodMerchantStatusResponse(
    string? OperationState, bool Available, IReadOnlyCollection<IFoodMerchantValidationResponse> Validations);
