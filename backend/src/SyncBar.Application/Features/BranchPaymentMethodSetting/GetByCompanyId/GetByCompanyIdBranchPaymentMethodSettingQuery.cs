using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByCompanyId
{
    public sealed record GetByCompanyIdBranchPaymentMethodSettingQuery(
        long CompanyId) : IQuery<BranchPaymentMethodSettingResponse?>;
}