using MediatR;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.GetPublicBill;

internal sealed class GetPublicBillQueryHandler(
    IDiningTableRepository tableRepository,
    ICustomerOrderRepository orderRepository,
    IProductRepository productRepository,
    ILogTrackerRepository logRepository) : IRequestHandler<GetPublicBillQuery, Result<PublicBillResponse>>
{
    public async Task<Result<PublicBillResponse>> Handle(GetPublicBillQuery request, CancellationToken ct)
    {
        var table = await tableRepository.GetByQrTokenAsync(request.Token, ct);
        if (table is null)
            return Result.Failure<PublicBillResponse>(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));
        var openOrder = await orderRepository.GetOpenByTableAsync(table.Id, ct);
        if (openOrder is null)
            return Result.Failure<PublicBillResponse>(new Error("Order.NotFound", "Nenhum pedido em aberto para esta mesa."));
        var productIds = openOrder.Items
            .Where(i => i.IsActive)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();
        var products = await productRepository.GetByIdsAsync(productIds, ct);
        var productDict = products.ToDictionary(p => p.Id, p => p.Name);
        var itemsResponse = openOrder.Items
            .Where(i => i.IsActive)
            .Select(i => new PublicBillItemResponse(
                i.Id,
                productDict.TryGetValue(i.ProductId, out var name) ? name : "Produto Desconhecido", // Nome real do produto
                i.Quantity,
                i.UnitPrice,
                i.TotalAmount,
                i.OrderItemStatusId,
                i.CreatedAt,
                i.Notes
            )).ToList();
        var response = new PublicBillResponse(
            openOrder.Id,
            table.Number.ToString(),
            openOrder.OrderStatusId.ToString(),
            openOrder.SubtotalAmount,
            openOrder.DiscountAmount,
            openOrder.ServiceFeeAmount,
            openOrder.TotalAmount,
            itemsResponse
        );
        return Result.Success(response);
    }
}