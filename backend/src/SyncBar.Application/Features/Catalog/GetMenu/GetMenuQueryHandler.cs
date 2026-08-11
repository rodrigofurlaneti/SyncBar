using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.GetMenu;

internal sealed class GetMenuQueryHandler(
    IProductRepository productRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetMenuQuery, IReadOnlyCollection<MenuItemResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<MenuItemResponse>>> Handle(
        GetMenuQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetMenuQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var products = await productRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

                // Ordenacao em C# — nunca ORDER BY em SqlQuery.
                IReadOnlyCollection<MenuItemResponse> response = products
                    .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
                    .Select(p => new MenuItemResponse(
                        p.Id, p.CategoryId, p.UnitOfMeasureId, p.Name, p.Description, p.Barcode,
                        p.SalePrice, p.CostPrice, p.IsStockControlled, p.PreparationTimeMinutes, p.ImageUrl))
                    .ToList();

                return Result.Success(response);
            });
}