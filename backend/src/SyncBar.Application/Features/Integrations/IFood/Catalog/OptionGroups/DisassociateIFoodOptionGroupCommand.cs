using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

// Fase 10 — desassocia um grupo de opções de um produto
// (DELETE catalog/v2.0/merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}).
public sealed record DisassociateIfoodOptionGroupCommand(long BranchId, Guid OptionGroupId, Guid ProductId) : ICommand;
