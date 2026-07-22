using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.GetCategories;

internal sealed class GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<CategoryResponse>>> Handle(
        GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

        IReadOnlyCollection<CategoryResponse> response = categories
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.DisplayOrder))
            .ToList();

        return Result.Success(response);
    }
}
