using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.UpdateComplementItem;

internal sealed class UpdateComplementItemCommandHandler : BaseCommandHandler<UpdateComplementItemCommand>
{
    private readonly IComplementItemRepository _complementItemRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateComplementItemCommandHandler(
        IComplementItemRepository complementItemRepository,
        IIfoodCatalogSyncTrigger catalogSyncTrigger,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _complementItemRepository = complementItemRepository;
        _catalogSyncTrigger = catalogSyncTrigger;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(UpdateComplementItemCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(UpdateComplementItemCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var complementItem = await _complementItemRepository.GetByIdForUpdateAsync(request.ComplementItemId, cancellationToken);
                if (complementItem is null || !complementItem.IsActive)
                    return Result.Failure(new Error("ComplementItem.NotFound", "Complement item not found."));

                var result = complementItem.UpdateName(request.Name);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);

                // O nome vira o `name` do product embrulhado por cada option que usa este
                // ComplementItem (ver IfoodComplementMapping) — precisa resincronizar.
                _catalogSyncTrigger.TriggerCompanySync(complementItem.CompanyId);

                return Result.Success();
            });
}
