using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.GetCategoriesForManagement;

internal sealed class GetCategoriesForManagementQueryHandler(
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetCategoriesForManagementQuery, IReadOnlyCollection<CategoryManagementResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<CategoryManagementResponse>>> Handle(
        GetCategoriesForManagementQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetCategoriesForManagementQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var categories = await categoryRepository.GetAllByCompanyAsync(request.CompanyId, cancellationToken);
                var products = await productRepository.GetAllByCompanyAsync(request.CompanyId, cancellationToken);

                var countByCategory = products
                    .GroupBy(p => p.CategoryId)
                    .ToDictionary(g => g.Key, g => g.Count());

                IReadOnlyCollection<CategoryManagementResponse> response = categories
                    .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                    .Select(c => new CategoryManagementResponse(
                        c.Id,
                        c.Name,
                        c.DisplayOrder,
                        c.IsActive,
                        countByCategory.TryGetValue(c.Id, out var count) ? count : 0))
                    .ToList();

                return Result.Success(response);
            });
}
