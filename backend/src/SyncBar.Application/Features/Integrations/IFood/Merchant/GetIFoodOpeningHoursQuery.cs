using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record GetIFoodOpeningHoursQuery(long BranchId) : IQuery<IFoodOpeningHoursResponse>;
