namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record IFoodInterruptionResponse(string Id, string? Description, DateTime Start, DateTime End);
