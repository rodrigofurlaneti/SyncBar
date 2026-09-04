using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Catalog;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Storefront.GetBranchMenu;

internal sealed class GetBranchMenuQueryHandler(
    IBranchRepository branchRepository,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductComplementGroupRepository productComplementGroupRepository,
    IComplementGroupRepository complementGroupRepository,
    IComplementItemRepository complementItemRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetBranchMenuQuery, BranchMenuResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<BranchMenuResponse>> Handle(GetBranchMenuQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetBranchMenuQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
                if (branch is null || !branch.IsActive)
                    return Result.Failure<BranchMenuResponse>(new Error("Branch.NotFound", "Branch not found or inactive."));
                var products = await productRepository.GetByCompanyAsync(branch.CompanyId, cancellationToken);
                var productIds = products.Select(p => p.Id).ToList();
                var categories = await categoryRepository.GetByCompanyAsync(branch.CompanyId, cancellationToken);
                var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);
                var complementsByProduct = await MenuComplementsBuilder.BuildAsync(
                    productIds, productComplementGroupRepository, complementGroupRepository, complementItemRepository,
                    productRepository, cancellationToken);
                var items = products
                    .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
                    .Select(p => new MenuItemResponse(
                        p.Id,
                        p.CategoryId,
                        categoryMap.TryGetValue(p.CategoryId, out var catName) ? catName : "Geral",
                        p.UnitOfMeasureId,
                        p.Name,
                        p.Description,
                        p.Barcode,
                        p.SalePrice,
                        p.CostPrice,
                        p.IsStockControlled,
                        p.PreparationTimeMinutes,
                        p.ImageUrl,
                        complementsByProduct.TryGetValue(p.Id, out var groups) ? groups : []))
                    .ToList();
                return Result.Success(new BranchMenuResponse(branch.Name, items));
            });
    }
}