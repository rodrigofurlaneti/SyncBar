using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByCompanyId
{
    internal sealed class GetByCompanyIdBranchPaymentMethodSettingQueryHandler
        : BaseQueryHandler<GetByCompanyIdBranchPaymentMethodSettingQuery, BranchPaymentMethodSettingResponse?>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;

        public GetByCompanyIdBranchPaymentMethodSettingQueryHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<BranchPaymentMethodSettingResponse?>> Handle(
            GetByCompanyIdBranchPaymentMethodSettingQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByCompanyIdBranchPaymentMethodSettingQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByCompanyIdAsync(
                        request.CompanyId,
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
                        setting.IsActive, setting.CreatedAt, setting.UpdatedAt);

                    return Result.Success<BranchPaymentMethodSettingResponse?>(response);
                });
        }
    }
}
