using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Preparation.GetQueue;

internal sealed class GetPreparationQueueQueryHandler(
    ICustomerOrderRepository orderRepository,
    IProductRepository productRepository,
    IDiningTableRepository diningTableRepository,
    IComandaRepository comandaRepository)
    : IQueryHandler<GetPreparationQueueQuery, IReadOnlyCollection<PreparationTicketResponse>>
{
    // Itens de bar (sem tempo de preparo cadastrado): tolerancia media para
    // pegar, montar o balde, gelo e deixar no balcao.
    private const int BarToleranceMinutes = 5;

    private static readonly long[] PendingStatuses =
    [
        OrderItemStatusIds.Lancado,
        OrderItemStatusIds.EnviadoCozinha,
        OrderItemStatusIds.EmPreparo,
        OrderItemStatusIds.Pronto,
    ];

    public async Task<Result<IReadOnlyCollection<PreparationTicketResponse>>> Handle(
        GetPreparationQueueQuery request, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
        var tables = await diningTableRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        var productIds = orders
            .SelectMany(o => o.Items)
            .Where(i => i.IsActive)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);

        var tickets = new List<PreparationTicketResponse>();
        foreach (var order in orders.OrderBy(o => o.OpenedAt))
        {
            var pendingItems = order.Items
                .Where(i => i.IsActive && PendingStatuses.Contains(i.OrderItemStatusId))
                .OrderBy(i => i.CreatedAt)
                .Select(i =>
                {
                    var product = products.FirstOrDefault(p => p.Id == i.ProductId);
                    var isBarItem = product?.PreparationTimeMinutes is null;
                    return new PreparationItemResponse(
                        i.Id,
                        i.ProductId,
                        product?.Name ?? $"Produto {i.ProductId}",
                        i.Quantity,
                        i.OrderItemStatusId,
                        i.Notes,
                        i.SentToKitchenAt ?? i.CreatedAt,
                        product?.PreparationTimeMinutes ?? BarToleranceMinutes,
                        isBarItem);
                })
                .ToList();

            if (pendingItems.Count == 0)
                continue;

            string? comandaCode = null;
            if (order.ComandaId.HasValue)
            {
                var comanda = await comandaRepository.GetByIdAsync(order.ComandaId.Value, cancellationToken);
                comandaCode = comanda?.Code;
            }

            tickets.Add(new PreparationTicketResponse(
                order.Id,
                order.DiningTableId.HasValue
                    ? tables.FirstOrDefault(t => t.Id == order.DiningTableId.Value)?.Number
                    : null,
                comandaCode,
                order.OpenedAt,
                pendingItems));
        }

        return Result.Success<IReadOnlyCollection<PreparationTicketResponse>>(tickets);
    }
}
