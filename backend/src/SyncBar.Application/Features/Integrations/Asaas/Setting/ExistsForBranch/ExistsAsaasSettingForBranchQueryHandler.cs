using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForBranch
{
    internal sealed class ExistsAsaasSettingForBranchQueryHandler
        : BaseQueryHandler<ExistsAsaasSettingForBranchQuery, bool>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;

        public ExistsAsaasSettingForBranchQueryHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsAsaasSettingForBranchQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsAsaasSettingForBranchQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _settingRepository.ExistsForBranchAsync(
                        request.BranchId,
                        cancellationToken);

                    return Result.Success(exists);
                });
        }
    }
}
