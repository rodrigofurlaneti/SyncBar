using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.Setting.Create
{
    internal sealed class CreateAsaasIntegrationSettingCommandHandler
        : BaseCommandHandler<CreateAsaasIntegrationSettingCommand, CreateAsaasIntegrationSettingResponse>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAsaasIntegrationSettingCommandHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateAsaasIntegrationSettingResponse>> Handle(
            CreateAsaasIntegrationSettingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateAsaasIntegrationSettingCommandHandler),
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
                        var scopeMsg = request.BranchId.HasValue
                            ? $"para a filial {request.BranchId.Value}"
                            : $"global para a empresa {request.CompanyId}";

                        return Result.Failure<CreateAsaasIntegrationSettingResponse>(
                            Error.Conflict(
                                "AsaasSetting.AlreadyExists",
                                $"Já existe uma configuração de integração do Asaas cadastrada {scopeMsg}."));
                    }

                    // 2. Cria a entidade no Domínio (onde a criptografia ou sanitização pode ser aplicada)
                    var settingResult = AsaasIntegrationSetting.Create(
                        request.CompanyId,
                        request.BranchId,
                        request.ApiKey,
                        request.WebhookToken,
                        request.Environment ?? "SANDBOX",
                        request.IsActive);

                    if (settingResult.IsFailure)
                        return Result.Failure<CreateAsaasIntegrationSettingResponse>(settingResult.Error);

                    var setting = settingResult.Value;

                    // 3. Persistência
                    await _settingRepository.AddAsync(setting, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateAsaasIntegrationSettingResponse(
                        setting.Id,
                        setting.CompanyId,
                        setting.BranchId,
                        setting.Environment,
                        setting.IsActive);

                    return Result.Success(response);
                });
        }
    }
}
