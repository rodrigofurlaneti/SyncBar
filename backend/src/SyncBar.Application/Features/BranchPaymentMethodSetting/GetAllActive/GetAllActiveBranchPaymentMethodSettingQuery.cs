using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive
{
    public sealed record GetAllActiveBranchPaymentMethodSettingQuery() : IQuery<IReadOnlyList<BranchPaymentMethodSettingResponse>>;
}