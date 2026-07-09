using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.GetMenu;

internal sealed class GetMenuQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetMenuQuery, IReadOnlyCollection<MenuItemResponse>>
{
    public async Task<Result<IReadOnlyCollection<MenuItemResponse>>> Handle(
        GetMenuQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

        // Ordenacao em C# — nunca ORDER BY em SqlQuery.
        IReadOnlyCollection<MenuItemResponse> response = products
            .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
            .Select(p => new MenuItemResponse(
                p.Id, p.CategoryId, p.Name, p.Description, p.SalePrice, p.PreparationTimeMinutes))
            .ToList();

        return Result.Success(response);
    }
}
