using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.AddItem;

// NOTA: não aplica promoções (Promotion) como o lançamento feito pelo garçom —
// simplificação da primeira versão do autoatendimento; fast-follow natural.
internal sealed class AddPublicOrderItemCommandHandler(
    IDiningTableRepository diningTableRepository,
    IBranchRepository branchRepository,
    IProductRepository productRepository,
    ICustomerOrderRepository orderRepository,
    IPrintingService printingService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddPublicOrderItemCommand, long>
{
    public async Task<Result<long>> Handle(AddPublicOrderItemCommand request, CancellationToken cancellationToken)
    {
        var table = await diningTableRepository.GetByQrTokenAsync(request.Token, cancellationToken);
        if (table is null || !table.IsActive)
            return Result.Failure<long>(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));

        var branch = await branchRepository.GetByIdAsync(table.BranchId, cancellationToken);
        if (branch is null || !branch.IsActive)
            return Result.Failure<long>(new Error("Branch.NotFound", "Branch not found."));
        if (!branch.SelfServiceEmployeeId.HasValue)
            return Result.Failure<long>(new Error("Branch.SelfServiceDisabled",
                "Self-service ordering is not enabled for this branch. Ask the manager to configure it."));

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive || product.CompanyId != branch.CompanyId)
            return Result.Failure<long>(new Error("Product.NotFound", "Product not found."));

        // Reaproveita (ou abre) o pedido da mesa — o cliente pode chamar isso várias vezes
        // (um item por toque) durante a mesma visita.
        var order = await orderRepository.GetOpenByTableForUpdateAsync(table.Id, cancellationToken);
        var isNewOrder = order is null;
        if (order is null)
        {
            var created = CustomerOrder.Create(
                table.BranchId, table.Id, null, branch.SelfServiceEmployeeId.Value,
                null, "Pedido via QR Code", null, OrderTypeIds.Mesa);
            if (created.IsFailure)
                return Result.Failure<long>(created.Error);

            order = created.Value;
            await orderRepository.AddAsync(order, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken); // garante Id antes de lançar o item
        }

        if (isNewOrder)
            table.ChangeStatus(TableStatusIds.Ocupada);

        var itemCountBefore = order.Items.Count;

        // employeeId nulo no item — identifica que foi o próprio cliente quem lançou.
        var added = order.AddItem(product.Id, product.SalePrice, request.Quantity, request.Notes, null);
        if (added.IsFailure)
            return Result.Failure<long>(added.Error);

        await unitOfWork.CommitAsync(cancellationToken);

        var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
        try
        {
            await printingService.PrintOrderItemsAsync(order.Id, newItemIds, cancellationToken);
        }
        catch
        {
            // silencioso: a cozinha também vê a fila de preparo pela tela (GetQueue).
        }

        return Result.Success(order.Id);
    }
}
