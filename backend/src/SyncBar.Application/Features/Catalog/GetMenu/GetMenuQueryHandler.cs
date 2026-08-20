using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.GetMenu;

internal sealed class GetMenuQueryHandler(
    IProductRepository productRepository,
    IProductComplementGroupRepository productComplementGroupRepository,
    IComplementGroupRepository complementGroupRepository,
    IComplementItemRepository complementItemRepository,
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
                var productIds = products.Select(p => p.Id).ToList();

                // Fase 6a (extensão): grupos de complemento por produto, resolvidos em lote pra
                // não gerar 1 query por produto — ver MenuComplementsBuilder.
                var complementsByProduct = await MenuComplementsBuilder.BuildAsync(
                    productIds, productComplementGroupRepository, complementGroupRepository, complementItemRepository, cancellationToken);

                // Ordenacao em C# — nunca ORDER BY em SqlQuery.
                IReadOnlyCollection<MenuItemResponse> response = products
                    .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
                    .Select(p => new MenuItemResponse(
                        p.Id, p.CategoryId, p.UnitOfMeasureId, p.Name, p.Description, p.Barcode,
                        p.SalePrice, p.CostPrice, p.IsStockControlled, p.PreparationTimeMinutes, p.ImageUrl,
                        complementsByProduct.TryGetValue(p.Id, out var groups) ? groups : []))
                    .ToList();

                return Result.Success(response);
            });
}
