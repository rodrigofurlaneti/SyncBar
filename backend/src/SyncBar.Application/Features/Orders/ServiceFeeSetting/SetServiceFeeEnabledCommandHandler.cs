using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using DomainServiceFeeSetting = SyncBar.Domain.Entities.ServiceFeeSetting;

namespace SyncBar.Application.Features.Orders.ServiceFeeSetting;

internal sealed class SetServiceFeeEnabledCommandHandler(
    IServiceFeeSettingRepository settingRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetServiceFeeEnabledCommand>
{
    public async Task<Result> Handle(SetServiceFeeEnabledCommand request, CancellationToken cancellationToken)
    {
        // Upsert por filial (espelha UQ_ServiceFeeSetting_BranchId filtrado).
        var setting = await settingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
        if (setting is null)
        {
            var created = DomainServiceFeeSetting.Create(request.BranchId, request.Enabled);
            if (created.IsFailure)
                return Result.Failure(created.Error);
            await settingRepository.AddAsync(created.Value, cancellationToken);
        }
        else
        {
            var updated = setting.SetEnabled(request.Enabled);
            if (updated.IsFailure)
                return updated;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
