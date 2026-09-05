using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyId
{
    internal sealed class GetAsaasSettingByCompanyIdQueryHandler
       : BaseQueryHandler<GetAsaasSettingByCompanyIdQuery, AsaasIntegrationSettingResponse>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;

        public GetAsaasSettingByCompanyIdQueryHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<AsaasIntegrationSettingResponse>> Handle(
            GetAsaasSettingByCompanyIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasSettingByCompanyIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Busca a configuração padrão/global da empresa (BranchId == null)
                    var setting = await _settingRepository.GetByCompanyIdAsync(
                        request.CompanyId,
                        cancellationToken);

                    if (setting is null)
                    {
                        return Result.Failure<AsaasIntegrationSettingResponse>(
                            Error.NotFound(
                                "AsaasSetting.NotFound",
                                $"Configuração global do Asaas não encontrada para a empresa {request.CompanyId}."));
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
