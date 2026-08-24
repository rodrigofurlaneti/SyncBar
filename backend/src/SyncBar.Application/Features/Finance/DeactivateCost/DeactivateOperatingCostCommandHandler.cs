using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.DeactivateCost;

internal sealed class DeactivateOperatingCostCommandHandler : BaseCommandHandler<DeactivateOperatingCostCommand>
{
    private readonly IOperatingCostRepository _costRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateOperatingCostCommandHandler(
        IOperatingCostRepository costRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _costRepository = costRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(DeactivateOperatingCostCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeactivateOperatingCostCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está executando a ação, preencha:

                var cost = await _costRepository.GetByIdForUpdateAsync(request.OperatingCostId, cancellationToken);
                if (cost is null || !cost.IsActive)
                    return Result.Failure(new Error("OperatingCost.NotFound", "Cost entry not found."));

                cost.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}