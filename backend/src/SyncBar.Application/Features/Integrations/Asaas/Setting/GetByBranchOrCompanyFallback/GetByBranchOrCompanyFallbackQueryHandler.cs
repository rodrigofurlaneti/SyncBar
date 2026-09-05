using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback
{
    internal sealed class GetByBranchOrCompanyFallbackQueryHandler
        : BaseQueryHandler<GetByBranchOrCompanyFallbackQuery, AsaasIntegrationSettingResponse>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;

        public GetByBranchOrCompanyFallbackQueryHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<AsaasIntegrationSettingResponse>> Handle(
            GetByBranchOrCompanyFallbackQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByBranchOrCompanyFallbackQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByBranchOrCompanyFallbackAsync(
                        request.CompanyId,
                        request.BranchId,
                        cancellationToken);

                    if (setting is null || !setting.IsActive)
                    {
                        return Result.Failure<AsaasIntegrationSettingResponse>(
                            Error.NotFound(
                                "AsaasSetting.NotFound",
                                $"Nenhuma configuração ativa do Asaas encontrada para a empresa {request.CompanyId}" +
                                (request.BranchId.HasValue ? $" ou filial {request.BranchId.Value}." : ".")));
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
