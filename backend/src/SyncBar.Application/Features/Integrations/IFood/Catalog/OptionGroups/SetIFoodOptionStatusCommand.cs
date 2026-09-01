using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

// Fase 10 — pausa/reativa uma opção (PATCH catalog/v2.0/merchants/{merchantId}/options/{optionId}/status).
public sealed record SetIfoodOptionStatusCommand(
    long BranchId, Guid OptionId, bool Available, string? ParentCustomizationOptionId)
    : ICommand;
