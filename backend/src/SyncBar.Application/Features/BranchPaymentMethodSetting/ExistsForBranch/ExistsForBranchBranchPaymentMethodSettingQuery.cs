using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForBranch
{
    public sealed record ExistsBranchPaymentMethodSettingForBranchQuery(
        long CompanyId,
        long BranchId) : IQuery<bool>;
}