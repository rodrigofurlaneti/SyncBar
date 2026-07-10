using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.SetTarget;

internal sealed class SetRevenueTargetCommandHandler(
    IRevenueTargetRepository targetRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetRevenueTargetCommand, long>
{
    public async Task<Result<long>> Handle(SetRevenueTargetCommand request, CancellationToken cancellationToken)
    {
        // Upsert: uma meta ativa por filial/mes (espelha UQ_RevenueTarget filtrado).
        var existing = await targetRepository.GetByBranchAndMonthForUpdateAsync(
            request.BranchId, request.ReferenceYear, request.ReferenceMonth, cancellationToken);

        if (existing is not null)
        {
            var updated = existing.UpdateAmount(request.TargetAmount);
            if (updated.IsFailure)
                return Result.Failure<long>(updated.Error);

            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Success(existing.Id);
        }

        var target = RevenueTarget.Create(
            request.BranchId, request.ReferenceYear, request.ReferenceMonth, request.TargetAmount);
        if (target.IsFailure)
            return Result.Failure<long>(target.Error);

        await targetRepository.AddAsync(target.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(target.Value.Id);
    }
}
