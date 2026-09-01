using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.GetMenuForManagement;

internal sealed class GetMenuForManagementQueryHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetMenuForManagementQuery, IReadOnlyCollection<ProductManagementResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<ProductManagementResponse>>> Handle(
        GetMenuForManagementQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetMenuForManagementQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var products = await productRepository.GetAllByCompanyAsync(request.CompanyId, cancellationToken);
                var categories = await categoryRepository.GetAllByCompanyAsync(request.CompanyId, cancellationToken);
                var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

                IReadOnlyCollection<ProductManagementResponse> response = products
                    .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
                    .Select(p => new ProductManagementResponse(
                        p.Id,
                        p.CategoryId,
                        categoryMap.TryGetValue(p.CategoryId, out var catName) ? catName : "Categoria removida",
                        p.UnitOfMeasureId,
                        p.Name,
                        p.Description,
                        p.Barcode,
                        p.SalePrice,
                        p.CostPrice,
                        p.IsStockControlled,
                        p.PreparationTimeMinutes,
                        p.IsActive,
                        p.ImageUrl))
                    .ToList();

                return Result.Success(response);
            });
}
