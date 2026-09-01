using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ActivateCategory;

internal sealed class ActivateCategoryCommandHandler : BaseCommandHandler<ActivateCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateCategoryCommandHandler(
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

    public override Task<Result> Handle(ActivateCategoryCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(ActivateCategoryCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var category = await _categoryRepository.GetByIdForUpdateAsync(request.CategoryId, cancellationToken);
                if (category is null)
                    return Result.Failure(new Error("Category.NotFound", "Category not found."));
                if (category.IsActive)
                    return Result.Success();

                category.Activate();
                await _unitOfWork.CommitAsync(cancellationToken);

                _catalogSyncTrigger.TriggerCompanySync(category.CompanyId);

                return Result.Success();
            });
}
