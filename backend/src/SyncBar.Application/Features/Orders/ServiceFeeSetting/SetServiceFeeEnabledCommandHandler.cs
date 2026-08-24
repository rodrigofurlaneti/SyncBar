using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using DomainServiceFeeSetting = SyncBar.Domain.Entities.ServiceFeeSetting;

namespace SyncBar.Application.Features.Orders.ServiceFeeSetting;

internal sealed class SetServiceFeeEnabledCommandHandler : BaseCommandHandler<SetServiceFeeEnabledCommand>
{
    private readonly IServiceFeeSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetServiceFeeEnabledCommandHandler(
        IServiceFeeSettingRepository settingRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SetServiceFeeEnabledCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetServiceFeeEnabledCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário ou administrador executando a ação, preencha:

                // Upsert por filial (espelha UQ_ServiceFeeSetting_BranchId filtrado).
                var setting = await _settingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
                if (setting is null)
                {
                    var created = DomainServiceFeeSetting.Create(request.BranchId, request.Enabled);
                    if (created.IsFailure)
                        return Result.Failure(created.Error);

                    await _settingRepository.AddAsync(created.Value, cancellationToken);
                }
                else
                {
                    var updated = setting.SetEnabled(request.Enabled);
                    if (updated.IsFailure)
                        return updated;
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}