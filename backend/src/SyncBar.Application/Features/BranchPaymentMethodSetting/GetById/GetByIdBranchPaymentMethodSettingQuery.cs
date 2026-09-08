using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetById
{
    public sealed record GetByIdBranchPaymentMethodSettingQuery(
        long Id) : IQuery<BranchPaymentMethodSettingResponse?>;
}