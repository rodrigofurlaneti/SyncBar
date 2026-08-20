using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

// Nulo = remove a customização (volta pra estimativa automática do iFood).
public sealed record SetIFoodPreparationTimeCommand(long BranchId, int? Minutes) : ICommand;
