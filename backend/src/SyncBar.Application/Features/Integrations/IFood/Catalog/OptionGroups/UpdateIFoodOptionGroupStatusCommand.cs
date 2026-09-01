using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

// Fase 10 — pausa/reativa um grupo de opções (PATCH catalog/v2.0/merchants/{merchantId}/optionGroups/{optionGroupId}/status).
public sealed record UpdateIfoodOptionGroupStatusCommand(long BranchId, Guid OptionGroupId, bool Available) : ICommand;
