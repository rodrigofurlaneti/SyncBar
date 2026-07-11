using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.AddItem;

internal sealed class AddOrderItemCommandHandler(
    ICustomerOrderRepository orderRepository,
    IProductRepository productRepository,
    IPromotionRepository promotionRepository,
    IPrintingService printingService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddOrderItemCommand>
{
    public async Task<Result> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure(new Error("Product.NotFound", "Product not found."));

        var itemCountBefore = order.Items.Count;

        // Promocao ativa AGORA (horario local do bar) para este produto?
        var promotions = await promotionRepository.GetByBranchAsync(order.BranchId, cancellationToken);
        var activePromotion = promotions.FirstOrDefault(promo =>
            promo.ProductId == product.Id && promo.IsActiveAt(DateTime.Now));

        // Desconto %: o preco ja entra reduzido e congelado, com a nota da promocao.
        var unitPrice = product.SalePrice;
        var notes = request.Notes;
        if (activePromotion?.PromotionTypeId == PromotionTypeIds.Desconto && activePromotion.DiscountRate is not null)
        {
            unitPrice = Math.Round(product.SalePrice * (1 - activePromotion.DiscountRate.Value), 2);
            var tag = $"🏷 {activePromotion.Name} (−{activePromotion.DiscountRate.Value:P0})";
            notes = string.IsNullOrWhiteSpace(notes) ? tag : $"{notes} · {tag}";
        }

        var result = order.AddItem(product.Id, unitPrice, request.Quantity, notes, request.EmployeeId);
        if (result.IsFailure)
            return result;

        // Em dobro: pedido DENTRO da janela ganha a mesma quantidade gratis,
        // em linha propria (audita, aparece no preparo e baixa estoque).
        if (activePromotion?.PromotionTypeId == PromotionTypeIds.EmDobro)
        {
            var bonus = order.AddItem(product.Id, 0m, request.Quantity,
                $"🎁 {activePromotion.Name}", request.EmployeeId);
            if (bonus.IsFailure)
                return bonus;
        }

        await unitOfWork.CommitAsync(cancellationToken);

        // Cupom de pedido (cozinha/bar) com os itens recem-lancados — apos o commit
        // os Ids existem; a impressao nunca falha o lancamento.
        var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
        await printingService.PrintOrderItemsAsync(order.Id, newItemIds, cancellationToken);

        return Result.Success();
    }
}
