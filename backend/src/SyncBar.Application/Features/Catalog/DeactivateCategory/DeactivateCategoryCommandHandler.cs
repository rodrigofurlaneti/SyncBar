using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.DeactivateCategory;

internal sealed class DeactivateCategoryCommandHandler : BaseCommandHandler<DeactivateCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateCategoryCommandHandler(
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

    public override Task<Result> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(DeactivateCategoryCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var category = await _categoryRepository.GetByIdForUpdateAsync(request.CategoryId, cancellationToken);
                if (category is null || !category.IsActive)
                    return Result.Failure(new Error("Category.NotFound", "Category not found."));

                // Soft delete só na Category — não toca em Product.CategoryId (sem cascade). Os
                // produtos já cadastrados nela continuam existindo e funcionando normalmente;
                // eles só deixam de listar essa categoria como opção em GetCategories (que já
                // filtra IsActive) para novos cadastros/edições. O front resolve o nome da
                // categoria de um produto órfão com um fallback ("Categoria {id}") — ver
                // ProductsPage.tsx.
                category.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);

                _catalogSyncTrigger.TriggerCompanySync(category.CompanyId);

                return Result.Success();
            });
}
