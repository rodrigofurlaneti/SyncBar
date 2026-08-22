using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

// Fase 10 — atualiza o nome de um grupo de opções (PATCH catalog/v2.0/merchants/{merchantId}/optionGroups/{optionGroupId}).
public sealed record UpdateIFoodOptionGroupCommand(long BranchId, Guid OptionGroupId, string Name) : ICommand;
