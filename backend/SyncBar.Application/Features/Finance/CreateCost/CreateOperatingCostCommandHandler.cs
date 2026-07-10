using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.CreateCost;

internal sealed class CreateOperatingCostCommandHandler(
    IOperatingCostRepository costRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOperatingCostCommand, long>
{
    public async Task<Result<long>> Handle(CreateOperatingCostCommand request, CancellationToken cancellationToken)
    {
        var cost = OperatingCost.Create(
            request.BranchId, request.CostTypeId, request.Description.Trim(),
            request.Amount, request.ReferenceYear, request.ReferenceMonth);
        if (cost.IsFailure)
            return Result.Failure<long>(cost.Error);

        await costRepository.AddAsync(cost.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(cost.Value.Id);
    }
}
