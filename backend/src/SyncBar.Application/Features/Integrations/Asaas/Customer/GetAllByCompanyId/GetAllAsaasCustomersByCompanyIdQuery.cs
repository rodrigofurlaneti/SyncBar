using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId
{
    public sealed record GetAllAsaasCustomersByCompanyIdQuery(
        long CompanyId) : IQuery<IReadOnlyList<AsaasIntegrationCustomerResponse>>;
}
