using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.AddItem;

internal sealed class AddOrderItemCommandHandler(
    ICustomerOrderRepository orderRepository,
    IProductRepository productRepository,
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

        // UnitPrice congelado aqui — preco vigente do cardapio no momento do lancamento.
        var result = order.AddItem(product.Id, product.SalePrice, request.Quantity, request.Notes, request.EmployeeId);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
