using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive
{
    internal sealed class GetAllActiveBranchPaymentMethodSettingQueryHandler
        : BaseQueryHandler<GetAllActiveBranchPaymentMethodSettingQuery, IReadOnlyList<BranchPaymentMethodSettingResponse>>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;

        public GetAllActiveBranchPaymentMethodSettingQueryHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<IReadOnlyList<BranchPaymentMethodSettingResponse>>> Handle(
            GetAllActiveBranchPaymentMethodSettingQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAllActiveBranchPaymentMethodSettingQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var settings = await _settingRepository.GetAllActiveAsync(cancellationToken);

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