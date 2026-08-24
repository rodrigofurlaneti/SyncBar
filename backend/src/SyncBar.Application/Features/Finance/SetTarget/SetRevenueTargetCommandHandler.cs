using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.SetTarget;

internal sealed class SetRevenueTargetCommandHandler : BaseCommandHandler<SetRevenueTargetCommand, long>
{
    private readonly IRevenueTargetRepository _targetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetRevenueTargetCommandHandler(
        IRevenueTargetRepository targetRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _targetRepository = targetRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(SetRevenueTargetCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetRevenueTargetCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está executando a ação, preencha:

                // Upsert: uma meta ativa por filial/mes (espelha UQ_RevenueTarget filtrado).
                var existing = await _targetRepository.GetByBranchAndMonthForUpdateAsync(
                    request.BranchId, request.ReferenceYear, request.ReferenceMonth, cancellationToken);

                if (existing is not null)
                {
                    var updated = existing.UpdateAmount(request.TargetAmount);
                    if (updated.IsFailure)
                        return Result.Failure<long>(updated.Error);

                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success(existing.Id);
                }

                var target = RevenueTarget.Create(
                    request.BranchId, request.ReferenceYear, request.ReferenceMonth, request.TargetAmount);
                if (target.IsFailure)
                    return Result.Failure<long>(target.Error);

                await _targetRepository.AddAsync(target.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(target.Value.Id);
            });
    }
}