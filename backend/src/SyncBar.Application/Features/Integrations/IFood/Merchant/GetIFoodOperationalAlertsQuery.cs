using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record GetIFoodOperationalAlertsQuery(long CompanyId) : IQuery<IReadOnlyCollection<IFoodOperationalAlertResponse>>;
