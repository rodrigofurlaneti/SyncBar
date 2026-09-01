using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.DeactivateComplementGroup;

internal sealed class DeactivateComplementGroupCommandHandler : BaseCommandHandler<DeactivateComplementGroupCommand>
{
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateComplementGroupCommandHandler(
        IComplementGroupRepository complementGroupRepository,
        IIfoodCatalogSyncTrigger catalogSyncTrigger,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _complementGroupRepository = complementGroupRepository;
        _catalogSyncTrigger = catalogSyncTrigger;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(DeactivateComplementGroupCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(DeactivateComplementGroupCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var complementGroup = await _complementGroupRepository.GetByIdForUpdateAsync(request.ComplementGroupId, cancellationToken);
                if (complementGroup is null || !complementGroup.IsActive)
                    return Result.Failure(new Error("ComplementGroup.NotFound", "Complement group not found."));

                complementGroup.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);

                // Grupo desativado precisa sumir/pausar no catálogo do Ifood pros produtos que o usam.
                _catalogSyncTrigger.TriggerCompanySync(complementGroup.CompanyId);

                return Result.Success();
            });
}
