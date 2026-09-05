using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchIdForUpdate
{
    internal sealed class GetAsaasSettingByBranchIdForUpdateQueryHandler
        : BaseQueryHandler<GetAsaasSettingByBranchIdForUpdateQuery, AsaasIntegrationSettingResponse>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;

        public GetAsaasSettingByBranchIdForUpdateQueryHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<AsaasIntegrationSettingResponse>> Handle(
            GetAsaasSettingByBranchIdForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasSettingByBranchIdForUpdateQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Busca a entidade com change tracking ativo para preparação de mutação
                    var setting = await _settingRepository.GetByBranchIdForUpdateAsync(
                        request.CompanyId,
                        request.BranchId,
                        cancellationToken);

                    if (setting is null)
                    {
                        return Result.Failure<AsaasIntegrationSettingResponse>(
                            Error.NotFound(
                                "AsaasSetting.NotFound",
                                $"Configuração de integração Asaas não encontrada para atualização na filial {request.BranchId} da empresa {request.CompanyId}."));
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
