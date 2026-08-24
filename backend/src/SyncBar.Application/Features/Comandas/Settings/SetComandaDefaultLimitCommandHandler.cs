using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Comandas.Settings;

internal sealed class SetComandaDefaultLimitCommandHandler : BaseCommandHandler<SetComandaDefaultLimitCommand>
{
    private readonly IComandaSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetComandaDefaultLimitCommandHandler(
        IComandaSettingRepository settingRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SetComandaDefaultLimitCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetComandaDefaultLimitCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP do request, se houver
            async (userIdBox) =>
            {
                // Upsert por filial (espelha UQ_ComandaSetting_BranchId filtrado).
                var setting = await _settingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
                if (setting is null)
                {
                    var created = ComandaSetting.Create(request.BranchId, request.DefaultLimitAmount);
                    if (created.IsFailure)
                        return Result.Failure(created.Error);

                    await _settingRepository.AddAsync(created.Value, cancellationToken);
                }
                else
                {
                    var updated = setting.Update(request.DefaultLimitAmount);
                    if (updated.IsFailure)
                        return updated;
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}