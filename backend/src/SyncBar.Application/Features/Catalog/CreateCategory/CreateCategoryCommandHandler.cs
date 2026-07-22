using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.CreateCategory;

internal sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCategoryCommand, long>
{
    public async Task<Result<long>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.CompanyId, request.Name, request.DisplayOrder);
        if (category.IsFailure)
            return Result.Failure<long>(category.Error);

        await categoryRepository.AddAsync(category.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(category.Value.Id);
    }
}
