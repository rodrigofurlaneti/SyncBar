namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record IfoodInterruptionResponse(string Id, string? Description, DateTime Start, DateTime End);
