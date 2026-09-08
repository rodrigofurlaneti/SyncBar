using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchOrCompanyFallback
{
    internal sealed class GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandler
        : BaseQueryHandler<GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery, BranchPaymentMethodSettingResponse?>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;

        public GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<BranchPaymentMethodSettingResponse?>> Handle(
            GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByBranchOrCompanyFallbackAsync(
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