using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood;

public sealed record GetIfoodSettingsQuery(long CompanyId) : IQuery<IfoodSettingsResponse>;
