using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood;

public sealed record GetIFoodMerchantMappingsQuery(long CompanyId) : IQuery<IReadOnlyCollection<IFoodMerchantMappingResponse>>;
