using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForCompany
{
    public sealed record ExistsKeetaSettingForCompanyQuery(
        long CompanyId) : IQuery<bool>;
}
