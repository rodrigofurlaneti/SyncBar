using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.RemoveComplement;

internal sealed class RemoveComplementCommandHandler : BaseCommandHandler<RemoveComplementCommand>
{
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IIFoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveComplementCommandHandler(
        IComplementGroupRepository complementGroupRepository,
        IIFoodCatalogSyncTrigger catalogSyncTrigger,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _complementGroupRepository = complementGroupRepository;
        _catalogSyncTrigger = catalogSyncTrigger;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(RemoveComplementCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(RemoveComplementCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var complementGroup = await _complementGroupRepository.GetByIdForUpdateAsync(request.ComplementGroupId, cancellationToken);
                if (complementGroup is null || !complementGroup.IsActive)
                    return Result.Failure(new Error("ComplementGroup.NotFound", "Complement group not found."));

                var result = complementGroup.RemoveComplement(request.ComplementId);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);

                _catalogSyncTrigger.TriggerCompanySync(complementGroup.CompanyId);

                return Result.Success();
            });
}
