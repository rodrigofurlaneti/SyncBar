namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record IFoodMerchantValidationResponse(string Id, string State, string? Message);

public sealed record IFoodMerchantStatusResponse(
    string? OperationState, IReadOnlyCollection<IFoodMerchantValidationResponse> Validations);
