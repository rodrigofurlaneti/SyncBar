using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.CreateCost;

internal sealed class CreateOperatingCostCommandHandler(
    IOperatingCostRepository costRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreateOperatingCostCommand, long>(logRepository, unitOfWork)
{
    public override async Task<Result<long>> Handle(CreateOperatingCostCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateOperatingCostCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário responsável pela ação, preencha:
                // userIdBox.Value = request.UserId;

                var cost = OperatingCost.Create(
                    request.BranchId, request.CostTypeId, request.Description.Trim(),
                    request.Amount, request.ReferenceYear, request.ReferenceMonth);
                if (cost.IsFailure)
                    return Result.Failure<long>(cost.Error);

                await costRepository.AddAsync(cost.Value, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(cost.Value.Id);
            });
    }
}