using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record IFoodShippingCancellationReasonResponse(string CancelCodeId, string Description);

public sealed record GetIFoodShippingCancellationReasonsQuery(long Id) : IQuery<IReadOnlyCollection<IFoodShippingCancellationReasonResponse>>;
