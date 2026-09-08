using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchOrCompanyFallback
{
    public sealed record GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery(
        long CompanyId,
        long? BranchId) : IQuery<BranchPaymentMethodSettingResponse?>;
}