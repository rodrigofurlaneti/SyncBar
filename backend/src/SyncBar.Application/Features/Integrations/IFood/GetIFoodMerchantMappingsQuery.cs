using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood;

public sealed record GetIfoodMerchantMappingsQuery(long CompanyId) : IQuery<IReadOnlyCollection<IfoodMerchantMappingResponse>>;
