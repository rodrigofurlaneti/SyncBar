using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForCompany
{
    internal sealed class ExistsKeetaSettingForCompanyQueryHandler
        : BaseQueryHandler<ExistsKeetaSettingForCompanyQuery, bool>
    {
        private readonly IKeetaIntegrationSettingRepository _settingRepository;

        public ExistsKeetaSettingForCompanyQueryHandler(
            IKeetaIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsKeetaSettingForCompanyQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsKeetaSettingForCompanyQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _settingRepository.ExistsForCompanyAsync(request.CompanyId, cancellationToken);
                    return Result.Success(exists);
                });
        }
    }
}
