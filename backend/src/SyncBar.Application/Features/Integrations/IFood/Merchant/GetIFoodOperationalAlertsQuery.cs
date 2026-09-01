using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record GetIfoodOperationalAlertsQuery(long CompanyId) : IQuery<IReadOnlyCollection<IfoodOperationalAlertResponse>>;
