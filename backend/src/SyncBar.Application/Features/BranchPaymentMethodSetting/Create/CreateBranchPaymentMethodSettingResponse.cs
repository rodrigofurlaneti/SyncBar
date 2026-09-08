namespace SyncBar.Application.Features.BranchPaymentMethodSetting.Create
{
    public sealed record CreateBranchPaymentMethodSettingResponse(
        long Id,
        long CompanyId,
        long? BranchId,
        bool EnablePix,
        bool EnableBoleto,
        bool EnableCreditCard,
        bool EnableDebitCard,
        bool EnableCashMachine,
        bool IsActive, DateTime? CreatedAt = null, DateTime? UpdatedAt = null);
}
