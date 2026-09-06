using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Update
{
    internal sealed class UpdateKeetaIntegrationSettingCommandHandler
        : BaseCommandHandler<UpdateKeetaIntegrationSettingCommand>
    {
        private readonly IKeetaIntegrationSettingRepository _settingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateKeetaIntegrationSettingCommandHandler(
            IKeetaIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateKeetaIntegrationSettingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateKeetaIntegrationSettingCommandHandler),
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

                    // Mantém os valores atuais quando o campo não é informado (atualização parcial)
                    var credentialsResult = setting.SaveCredentials(
                        request.ClientId ?? setting.ClientId ?? string.Empty,
                        request.ClientSecret ?? setting.ClientSecret ?? string.Empty,
                        request.AppId ?? setting.AppId ?? string.Empty,
                        request.BaseUrl ?? setting.BaseUrl);

                    if (credentialsResult.IsFailure)
                        return Result.Failure(credentialsResult.Error);

                    _settingRepository.Update(setting);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
