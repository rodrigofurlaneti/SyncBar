using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive
{
    internal sealed class GetAllActiveAsaasSettingsQueryHandler
        : BaseQueryHandler<GetAllActiveAsaasSettingsQuery, IReadOnlyList<AsaasIntegrationSettingResponse>>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;

        public GetAllActiveAsaasSettingsQueryHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<IReadOnlyList<AsaasIntegrationSettingResponse>>> Handle(
            GetAllActiveAsaasSettingsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAllActiveAsaasSettingsQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var settings = await _settingRepository.GetAllActiveByCompanyIdAsync(
                        request.CompanyId,
                        cancellationToken);

                    var response = settings
                        .Select(s => new AsaasIntegrationSettingResponse(
                            s.Id,
                            s.CompanyId,
                            s.BranchId,
                            s.Environment,
                            s.IsActive,
                            s.CreatedAt,
                            s.UpdatedAt))
                        .ToList();

                    return Result.Success<IReadOnlyList<AsaasIntegrationSettingResponse>>(response);
                });
        }
    }
}
