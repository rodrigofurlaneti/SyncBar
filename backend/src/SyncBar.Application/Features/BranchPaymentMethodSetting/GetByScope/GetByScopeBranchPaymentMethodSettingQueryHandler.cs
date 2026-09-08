using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByScope
{
    internal sealed class GetByScopeBranchPaymentMethodSettingQueryHandler
        : BaseQueryHandler<GetByScopeBranchPaymentMethodSettingQuery, BranchPaymentMethodSettingResponse?>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;

        public GetByScopeBranchPaymentMethodSettingQueryHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<BranchPaymentMethodSettingResponse?>> Handle(
            GetByScopeBranchPaymentMethodSettingQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByScopeBranchPaymentMethodSettingQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByScopeAsync(
                        request.CompanyId,
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