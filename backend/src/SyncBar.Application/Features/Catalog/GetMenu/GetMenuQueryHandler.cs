using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.GetMenu;

internal sealed class GetMenuQueryHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository, // <-- 1. Injetado o repositório de categorias
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
            null,
            async (userIdBox) =>
            {
                var products = await productRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var productIds = products.Select(p => p.Id).ToList();

                // 2. Busca as categorias da empresa e monta o dicionário de ID -> Nome
                var categories = await categoryRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

                var complementsByProduct = await MenuComplementsBuilder.BuildAsync(
                    productIds, productComplementGroupRepository, complementGroupRepository, complementItemRepository,
                    productRepository, cancellationToken);

                IReadOnlyCollection<MenuItemResponse> response = products
                    .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
                    .Select(p => new MenuItemResponse(
                        p.Id,
                        p.CategoryId,
                        categoryMap.TryGetValue(p.CategoryId, out var catName) ? catName : "Geral", // <-- 3. Passando o Nome da Categoria
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

                return Result.Success(response);
            });
}