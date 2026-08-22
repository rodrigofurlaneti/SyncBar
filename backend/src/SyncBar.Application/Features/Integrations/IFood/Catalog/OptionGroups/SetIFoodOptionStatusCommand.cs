using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

// Fase 10 — pausa/reativa uma opção (PATCH catalog/v2.0/merchants/{merchantId}/options/{optionId}/status).
public sealed record SetIFoodOptionStatusCommand(
    long BranchId, Guid OptionId, bool Available, string? ParentCustomizationOptionId)
    : ICommand;
