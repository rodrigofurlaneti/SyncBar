using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood;

public sealed record GetIFoodSettingsQuery(long CompanyId) : IQuery<IFoodSettingsResponse>;
