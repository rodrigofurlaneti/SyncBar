using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForCompany
{
    public sealed record ExistsBranchPaymentMethodSettingForCompanyQuery(
        long CompanyId) : IQuery<bool>;
}