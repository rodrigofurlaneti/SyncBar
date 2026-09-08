using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.Delete
{
    public sealed record DeleteBranchPaymentMethodSettingCommand(
        long Id,
        long CompanyId) : ICommand;
}