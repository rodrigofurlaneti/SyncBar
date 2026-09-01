using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

// Fase 10 — atualiza o nome de um grupo de opções (PATCH catalog/v2.0/merchants/{merchantId}/optionGroups/{optionGroupId}).
public sealed record UpdateIfoodOptionGroupCommand(long BranchId, Guid OptionGroupId, string Name) : ICommand;
