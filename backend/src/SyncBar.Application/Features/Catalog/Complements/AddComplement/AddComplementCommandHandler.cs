using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.AddComplement;

internal sealed class AddComplementCommandHandler : BaseCommandHandler<AddComplementCommand, long>
{
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IComplementItemRepository _complementItemRepository;
    private readonly IIFoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public AddComplementCommandHandler(
        IComplementGroupRepository complementGroupRepository,
        IComplementItemRepository complementItemRepository,
        IIFoodCatalogSyncTrigger catalogSyncTrigger,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _complementGroupRepository = complementGroupRepository;
        _complementItemRepository = complementItemRepository;
        _catalogSyncTrigger = catalogSyncTrigger;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<long>> Handle(AddComplementCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(AddComplementCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var complementGroup = await _complementGroupRepository.GetByIdForUpdateAsync(request.ComplementGroupId, cancellationToken);
                if (complementGroup is null || !complementGroup.IsActive)
                    return Result.Failure<long>(new Error("ComplementGroup.NotFound", "Complement group not found."));

                var complementItem = await _complementItemRepository.GetByIdAsync(request.ComplementItemId, cancellationToken);
                if (complementItem is null || !complementItem.IsActive || complementItem.CompanyId != complementGroup.CompanyId)
                    return Result.Failure<long>(new Error("ComplementItem.NotFound", "Complement item not found for this company."));

                var result = complementGroup.AddComplement(request.ComplementItemId, request.ExtraPrice);
                if (result.IsFailure)
                    return Result.Failure<long>(result.Error);

                await _unitOfWork.CommitAsync(cancellationToken);

                _catalogSyncTrigger.TriggerCompanySync(complementGroup.CompanyId);

                return Result.Success(result.Value.Id);
            });
}
