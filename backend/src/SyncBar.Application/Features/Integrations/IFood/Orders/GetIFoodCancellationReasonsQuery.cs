using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed record IfoodCancellationReasonResponse(string Code, string Description);

public sealed record GetIfoodCancellationReasonsQuery(long IfoodOrderId) : IQuery<IReadOnlyCollection<IfoodCancellationReasonResponse>>;
