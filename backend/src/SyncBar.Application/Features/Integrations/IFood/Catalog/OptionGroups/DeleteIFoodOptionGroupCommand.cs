using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

// Fase 10 — exclui um grupo de opções (DELETE catalog/v2.0/merchants/{merchantId}/optionGroups/{optionGroupId}).
public sealed record DeleteIFoodOptionGroupCommand(long BranchId, Guid OptionGroupId) : ICommand;
