using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByScope
{
    public sealed record GetByScopeBranchPaymentMethodSettingQuery(
        long CompanyId,
        long? BranchId) : IQuery<BranchPaymentMethodSettingResponse?>;
}