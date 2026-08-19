using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed record IFoodCancellationReasonResponse(string Code, string Description);

public sealed record GetIFoodCancellationReasonsQuery(long IFoodOrderId) : IQuery<IReadOnlyCollection<IFoodCancellationReasonResponse>>;
