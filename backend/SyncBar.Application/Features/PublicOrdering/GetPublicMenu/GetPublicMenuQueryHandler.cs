using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Catalog;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.GetPublicMenu;

internal sealed class GetPublicMenuQueryHandler(
    IDiningTableRepository diningTableRepository,
    IBranchRepository branchRepository,
    IProductRepository productRepository)
    : IQueryHandler<GetPublicMenuQuery, PublicMenuResponse>
{
    public async Task<Result<PublicMenuResponse>> Handle(GetPublicMenuQuery request, CancellationToken cancellationToken)
    {
        var table = await diningTableRepository.GetByQrTokenAsync(request.Token, cancellationToken);
        if (table is null)
            return Result.Failure<PublicMenuResponse>(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));

        var branch = await branchRepository.GetByIdAsync(table.BranchId, cancellationToken);
        if (branch is null || !branch.IsActive)
            return Result.Failure<PublicMenuResponse>(new Error("Branch.NotFound", "Branch not found."));

        var products = await productRepository.GetByCompanyAsync(branch.CompanyId, cancellationToken);

        var items = products
            .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
            .Select(p => new MenuItemResponse(
                p.Id, p.CategoryId, p.UnitOfMeasureId, p.Name, p.Description, p.Barcode,
                p.SalePrice, p.CostPrice, p.IsStockControlled, p.PreparationTimeMinutes, p.ImageUrl))
            .ToList();

        return Result.Success(new PublicMenuResponse(branch.Name, table.Number, items));
    }
}
