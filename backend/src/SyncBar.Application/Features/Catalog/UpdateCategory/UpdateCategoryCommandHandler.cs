using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.UpdateCategory;

internal sealed class UpdateCategoryCommandHandler : BaseCommandHandler<UpdateCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IIfoodCatalogSyncTrigger catalogSyncTrigger,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _catalogSyncTrigger = catalogSyncTrigger;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(UpdateCategoryCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                var category = await _categoryRepository.GetByIdForUpdateAsync(request.CategoryId, cancellationToken);
                if (category is null || !category.IsActive)
                    return Result.Failure(new Error("Category.NotFound", "Category not found."));

                var result = category.UpdateDetails(request.Name, request.DisplayOrder);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);

                _catalogSyncTrigger.TriggerCompanySync(category.CompanyId);

                return Result.Success();
            });
}
