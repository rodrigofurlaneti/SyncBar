using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForBranch
{
    internal sealed class ExistsBranchPaymentMethodSettingForBranchQueryHandler
        : BaseQueryHandler<ExistsBranchPaymentMethodSettingForBranchQuery, bool>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;

        public ExistsBranchPaymentMethodSettingForBranchQueryHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsBranchPaymentMethodSettingForBranchQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsBranchPaymentMethodSettingForBranchQueryHandler),
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