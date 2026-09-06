using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Create
{
    internal sealed class CreateKeetaIntegrationSettingCommandHandler
        : BaseCommandHandler<CreateKeetaIntegrationSettingCommand, CreateKeetaIntegrationSettingResponse>
    {
        private readonly IKeetaIntegrationSettingRepository _settingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateKeetaIntegrationSettingCommandHandler(
            IKeetaIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateKeetaIntegrationSettingResponse>> Handle(
            CreateKeetaIntegrationSettingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateKeetaIntegrationSettingCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // 1. Evita duplicidade de configuração para o mesmo escopo (Empresa ou Filial específica)
                    var existingSetting = await _settingRepository.GetByScopeAsync(
                        request.CompanyId,
                        request.BranchId,
                        cancellationToken);

                    if (existingSetting is not null)
                    {
                        var scopeMsg = request.BranchId > 0
                            ? $"para a filial {request.BranchId}"
                            : $"global para a empresa {request.CompanyId}";

                        return Result.Failure<CreateKeetaIntegrationSettingResponse>(
                            Error.Conflict(
                                "KeetaSetting.AlreadyExists",
                                $"Já existe uma configuração de integração do Keeta cadastrada {scopeMsg}."));
                    }

                    // 2. Cria a entidade no Domínio
                    var settingResult = KeetaIntegrationSetting.Create(request.CompanyId, request.BranchId);
                    if (settingResult.IsFailure)
                        return Result.Failure<CreateKeetaIntegrationSettingResponse>(settingResult.Error);

                    var setting = settingResult.Value;

                    // 3. Credenciais do OAuth client_credentials
                    var credentialsResult = setting.SaveCredentials(
                        request.ClientId,
                        request.ClientSecret,
                        request.AppId,
                        request.BaseUrl);

                    if (credentialsResult.IsFailure)
                        return Result.Failure<CreateKeetaIntegrationSettingResponse>(credentialsResult.Error);

                    // 4. Persistência
                    await _settingRepository.AddAsync(setting, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateKeetaIntegrationSettingResponse(
                        setting.Id,
                        setting.CompanyId,
                        setting.BranchId,
                        setting.BaseUrl);

                    return Result.Success(response);
                });
        }
    }
}
