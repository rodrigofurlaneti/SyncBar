using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActiveByCompanyId
{
    internal sealed class GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryHandler
        : BaseQueryHandler<GetAllActiveByCompanyIdBranchPaymentMethodSettingQuery, IReadOnlyList<BranchPaymentMethodSettingResponse>>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;

        public GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<IReadOnlyList<BranchPaymentMethodSettingResponse>>> Handle(
            GetAllActiveByCompanyIdBranchPaymentMethodSettingQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAllActiveByCompanyIdBranchPaymentMethodSettingQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var settings = await _settingRepository.GetAllActiveByCompanyIdAsync(
                        request.CompanyId,
                        cancellationToken);

                    var response = settings.Select(s => new BranchPaymentMethodSettingResponse(
                        s.Id,
                        s.CompanyId,
                        s.BranchId,
                        s.EnablePix,
                        s.EnableBoleto,
                        s.EnableCreditCard,
                        s.EnableDebitCard,
                        s.EnableCashMachine,
                        s.IsActive
                    )).ToList();

                    return Result.Success<IReadOnlyList<BranchPaymentMethodSettingResponse>>(response);
                });
        }
    }
}