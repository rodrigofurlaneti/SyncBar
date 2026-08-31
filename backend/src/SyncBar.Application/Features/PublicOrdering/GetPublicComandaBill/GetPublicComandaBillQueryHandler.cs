using MediatR;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.GetPublicComandaBill;

internal sealed class GetPublicComandaBillQueryHandler(
    IDiningTableRepository tableRepository,
    IComandaRepository comandaRepository,
    ICustomerOrderRepository orderRepository,
    IProductRepository productRepository,
    ILogTrackerRepository logRepository) : IRequestHandler<GetPublicComandaBillQuery, Result<PublicComandaBillResponse>>
{
    public async Task<Result<PublicComandaBillResponse>> Handle(GetPublicComandaBillQuery request, CancellationToken ct)
    {
        var table = await tableRepository.GetByQrTokenAsync(request.TableToken, ct);
        if (table is null)
            return Result.Failure<PublicComandaBillResponse>(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));
        var comanda = await comandaRepository.GetByCodeAsync(table.BranchId, request.ComandaCode, ct);
        if (comanda is null || !comanda.IsActive)
            return Result.Failure<PublicComandaBillResponse>(new Error("Comanda.NotFound", "Comanda não encontrada."));
        var openOrder = await orderRepository.GetOpenByComandaAsync(comanda.Id, ct);
        if (openOrder is null)
            return Result.Failure<PublicComandaBillResponse>(new Error("Order.NotFound", "Nenhum consumo em aberto para esta comanda."));
        var productIds = openOrder.Items
            .Where(i => i.IsActive)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();
        var products = await productRepository.GetByIdsAsync(productIds, ct);
        var productDict = products.ToDictionary(p => p.Id, p => p.Name);
        var itemsResponse = openOrder.Items
            .Where(i => i.IsActive)
            .Select(i => new PublicComandaBillItemResponse(
                i.Id,
                productDict.TryGetValue(i.ProductId, out var name) ? name : "Produto",
                i.Quantity,
                i.UnitPrice,
                i.TotalAmount,
                i.OrderItemStatusId,
                i.CreatedAt,
                i.Notes
            )).ToList();
        var response = new PublicComandaBillResponse(
            openOrder.Id,
            comanda.Code,
            openOrder.OrderStatusId.ToString(),
            openOrder.SubtotalAmount,
            openOrder.DiscountAmount,
            openOrder.ServiceFeeAmount,
            openOrder.TotalAmount,
            openOrder.CreditLimitAmount,
            itemsResponse
        );
        return Result.Success(response);
    }
}