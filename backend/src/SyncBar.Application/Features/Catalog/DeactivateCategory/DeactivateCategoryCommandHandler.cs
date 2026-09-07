using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.DeactivateCategory;

internal sealed class DeactivateCategoryCommandHandler : BaseCommandHandler<DeactivateCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IIfoodCatalogSyncTrigger catalogSyncTrigger,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
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

                // Regra de negócio: uma categoria só pode ser desativada quando NENHUM produto
                // ativo estiver vinculado a ela — força o usuário a desativar os produtos primeiro
                // (produto já inativo não bloqueia). CreateProductCommandHandler já exige categoria
                // ativa para vincular um produto novo, então essa checagem fecha o ciclo: uma
                // categoria inativa nunca ganha produto ativo novo, e só fica inativa quando já
                // não tinha nenhum.
                var hasActiveProducts = await _productRepository.ExistsActiveByCategoryAsync(category.Id, cancellationToken);
                if (hasActiveProducts)
                    return Result.Failure(new Error(
                        "Category.HasLinkedProducts",
                        "Não é possível desativar esta categoria enquanto houver produtos ativos vinculados a ela. Desative os produtos primeiro."));

                category.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);

                _catalogSyncTrigger.TriggerCompanySync(category.CompanyId);

                return Result.Success();
            });
}
