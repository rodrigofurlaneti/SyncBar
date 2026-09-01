using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed record IfoodShippingCancellationReasonResponse(string CancelCodeId, string Description);

public sealed record GetIfoodShippingCancellationReasonsQuery(long Id) : IQuery<IReadOnlyCollection<IfoodShippingCancellationReasonResponse>>;
