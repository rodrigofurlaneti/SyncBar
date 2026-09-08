using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.Create
{
    public sealed record CreateBranchPaymentMethodSettingCommand(
        long CompanyId,
        long? BranchId,
        bool EnablePix = true,
        bool EnableBoleto = true,
        bool EnableCreditCard = true,
        bool EnableDebitCard = true,
        bool EnableCashMachine = true,
        bool IsActive = true) : ICommand<CreateBranchPaymentMethodSettingResponse>;
}