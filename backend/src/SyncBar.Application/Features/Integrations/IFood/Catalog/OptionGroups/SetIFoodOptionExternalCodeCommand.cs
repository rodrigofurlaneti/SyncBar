using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

// Fase 10 — atualiza o código externo de uma opção (PUT catalog/v2.0/merchants/{merchantId}/options/{optionId}/externalCode).
public sealed record SetIfoodOptionExternalCodeCommand(
    long BranchId, Guid OptionId, string ExternalCode, string? ParentCustomizationOptionId)
    : ICommand;
