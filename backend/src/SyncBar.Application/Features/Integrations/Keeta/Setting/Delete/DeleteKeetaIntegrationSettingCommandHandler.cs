using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Delete
{
    internal sealed class DeleteKeetaIntegrationSettingCommandHandler
        : BaseCommandHandler<DeleteKeetaIntegrationSettingCommand>
    {
        private readonly IKeetaIntegrationSettingRepository _settingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteKeetaIntegrationSettingCommandHandler(
            IKeetaIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            DeleteKeetaIntegrationSettingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteKeetaIntegrationSettingCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (setting is null || setting.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "KeetaSetting.NotFound",
                                $"Configuração de integração Keeta com ID {request.Id} não foi encontrada para esta empresa."));
                    }

                    _settingRepository.Delete(setting);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
