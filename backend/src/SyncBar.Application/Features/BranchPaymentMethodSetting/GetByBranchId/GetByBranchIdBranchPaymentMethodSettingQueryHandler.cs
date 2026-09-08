using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchId
{
    internal sealed class GetByBranchIdBranchPaymentMethodSettingQueryHandler
        : BaseQueryHandler<GetByBranchIdBranchPaymentMethodSettingQuery, BranchPaymentMethodSettingResponse?>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;

        public GetByBranchIdBranchPaymentMethodSettingQueryHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<BranchPaymentMethodSettingResponse?>> Handle(
            GetByBranchIdBranchPaymentMethodSettingQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByBranchIdBranchPaymentMethodSettingQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByBranchIdAsync(
                        request.BranchId,
                        cancellationToken);

                    if (setting is null)
                        return Result.Success<BranchPaymentMethodSettingResponse?>(null);

                    var response = new BranchPaymentMethodSettingResponse(
                        setting.Id,
                        setting.CompanyId,
                        setting.BranchId,
                        setting.EnablePix,
                        setting.EnableBoleto,
                        setting.EnableCreditCard,
                        setting.EnableDebitCard,
                        setting.EnableCashMachine,
                        setting.IsActive
                    );

                    return Result.Success<BranchPaymentMethodSettingResponse?>(response);
                });
        }
    }
}