using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForCompany
{
    public sealed record ExistsAsaasSettingForCompanyQuery(
        long CompanyId) : IQuery<bool>;
}
