using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActiveByCompanyId
{
    public sealed record GetAllActiveByCompanyIdBranchPaymentMethodSettingQuery(
        long CompanyId) : IQuery<IReadOnlyList<BranchPaymentMethodSettingResponse>>;
}