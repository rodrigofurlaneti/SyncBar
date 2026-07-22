using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Comandas.Settings;

internal sealed class SetComandaDefaultLimitCommandHandler(
    IComandaSettingRepository settingRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetComandaDefaultLimitCommand>
{
    public async Task<Result> Handle(SetComandaDefaultLimitCommand request, CancellationToken cancellationToken)
    {
        // Upsert por filial (espelha UQ_ComandaSetting_BranchId filtrado).
        var setting = await settingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
        if (setting is null)
        {
            var created = ComandaSetting.Create(request.BranchId, request.DefaultLimitAmount);
            if (created.IsFailure)
                return Result.Failure(created.Error);
            await settingRepository.AddAsync(created.Value, cancellationToken);
        }
        else
        {
            var updated = setting.Update(request.DefaultLimitAmount);
            if (updated.IsFailure)
                return updated;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
