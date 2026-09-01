using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.GetCategoryById;

internal sealed class GetCategoryByIdQueryHandler(
    ICategoryRepository categoryRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetCategoryByIdQuery, CategoryResponse>(logRepository, unitOfWork)
{
    public override Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetCategoryByIdQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (category is null || !category.IsActive)
                    return Result.Failure<CategoryResponse>(new Error("Category.NotFound", "Category not found."));

                var response = new CategoryResponse(category.Id, category.Name, category.DisplayOrder);
                return Result.Success(response);
            });
}
