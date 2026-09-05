using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByIdForUpdate
{
    internal sealed class GetAsaasSettingByIdForUpdateQueryHandler
        : BaseQueryHandler<GetAsaasSettingByIdForUpdateQuery, AsaasIntegrationSettingResponse>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;

        public GetAsaasSettingByIdForUpdateQueryHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<AsaasIntegrationSettingResponse>> Handle(
            GetAsaasSettingByIdForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasSettingByIdForUpdateQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByIdForUpdateAsync(
                        request.Id,
                        cancellationToken);

                    if (setting is null)
                    {
                        return Result.Failure<AsaasIntegrationSettingResponse>(
                            Error.NotFound(
                                "AsaasSetting.NotFound",
                                $"Configuração de integração Asaas com ID {request.Id} não foi encontrada para atualização."));
                    }

                    var response = new AsaasIntegrationSettingResponse(
                        setting.Id,
                        setting.CompanyId,
                        setting.BranchId,
                        setting.Environment,
                        setting.IsActive,
                        setting.CreatedAt,
                        setting.UpdatedAt);

                    return Result.Success(response);
                });
        }
    }
}
