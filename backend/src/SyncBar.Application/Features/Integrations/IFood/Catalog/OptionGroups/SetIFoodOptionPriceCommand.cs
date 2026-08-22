using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

// Fase 10 — atualiza o preço de uma opção (PUT catalog/v2.0/merchants/{merchantId}/options/{optionId}/price).
public sealed record SetIFoodOptionPriceCommand(
    long BranchId, Guid OptionId, decimal Value, decimal? OriginalValue, string? ParentCustomizationOptionId)
    : ICommand;
