using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForCompany
{
    internal sealed class ExistsAsaasSettingForCompanyQueryHandler
        : BaseQueryHandler<ExistsAsaasSettingForCompanyQuery, bool>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;

        public ExistsAsaasSettingForCompanyQueryHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsAsaasSettingForCompanyQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsAsaasSettingForCompanyQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _settingRepository.ExistsForCompanyAsync(
                        request.CompanyId,
                        cancellationToken);

                    return Result.Success(exists);
                });
        }
    }
}
