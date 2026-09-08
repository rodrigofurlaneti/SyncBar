using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchId
{
    public sealed record GetByBranchIdBranchPaymentMethodSettingQuery(
        long BranchId) : IQuery<BranchPaymentMethodSettingResponse?>;
}