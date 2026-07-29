using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.AddItem;

internal sealed class AddPublicOrderItemCommandHandler(
    IDiningTableRepository diningTableRepository,
    IBranchRepository branchRepository,
    IProductRepository productRepository,
    ICustomerOrderRepository orderRepository,
    IPrintingService printingService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) // Injecao do TimeProvider adicionada
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

        var currentTime = timeProvider.GetLocalNow().DateTime;

        var order = await orderRepository.GetOpenByTableForUpdateAsync(table.Id, cancellationToken);
        var isNewOrder = order is null;
        if (order is null)
        {
            // Passando o currentTime para o Create
            var created = CustomerOrder.Create(
                table.BranchId, table.Id, null, branch.SelfServiceEmployeeId.Value,
                null, "Pedido via QR Code", currentTime, null, OrderTypeIds.Mesa);

            if (created.IsFailure)
                return Result.Failure<long>(created.Error);

            order = created.Value;
            await orderRepository.AddAsync(order, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        if (isNewOrder)
            table.ChangeStatus(TableStatusIds.Ocupada);

        var itemCountBefore = order.Items.Count;

        // Passando o currentTime para o AddItem
        var added = order.AddItem(product.Id, product.SalePrice, request.Quantity, request.Notes, null, currentTime);
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
            // silencioso
        }

        return Result.Success(order.Id);
    }
}