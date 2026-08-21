using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using DomainIFoodMerchantMapping = SyncBar.Domain.Entities.IFoodMerchantMapping;

namespace SyncBar.Application.Features.Integrations.IFood;

internal sealed class SetIFoodMerchantMappingCommandHandler : BaseCommandHandler<SetIFoodMerchantMappingCommand>
{
    private readonly IIFoodMerchantMappingRepository _mappingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetIFoodMerchantMappingCommandHandler(
        IIFoodMerchantMappingRepository mappingRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _mappingRepository = mappingRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SetIFoodMerchantMappingCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIFoodMerchantMappingCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Upsert por filial — mesmo padrão do ServiceFeeSetting.
                var mapping = await _mappingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
                if (mapping is null)
                {
                    var created = DomainIFoodMerchantMapping.Create(request.BranchId);
                    if (created.IsFailure)
                        return Result.Failure(created.Error);

                    var set = created.Value.SetMerchant(request.MerchantId, request.MerchantUuid);
                    if (set.IsFailure)
                        return set;

                    await _mappingRepository.AddAsync(created.Value, cancellationToken);
                }
                else
                {
                    var set = mapping.SetMerchant(request.MerchantId, request.MerchantUuid);
                    if (set.IsFailure)
                        return set;
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
