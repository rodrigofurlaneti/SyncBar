using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.UpdateComplementGroup;

internal sealed class UpdateComplementGroupCommandHandler : BaseCommandHandler<UpdateComplementGroupCommand>
{
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateComplementGroupCommandHandler(
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

    public override Task<Result> Handle(UpdateComplementGroupCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(UpdateComplementGroupCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var complementGroup = await _complementGroupRepository.GetByIdForUpdateAsync(request.ComplementGroupId, cancellationToken);
                if (complementGroup is null || !complementGroup.IsActive)
                    return Result.Failure(new Error("ComplementGroup.NotFound", "Complement group not found."));

                var result = complementGroup.UpdateDetails(
                    request.Name, request.ComplementGroupTypeId, request.MinSelection, request.MaxSelection);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);

                _catalogSyncTrigger.TriggerCompanySync(complementGroup.CompanyId);

                return Result.Success();
            });
}
