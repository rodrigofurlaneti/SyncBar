using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId
{
    public sealed record GetByCustomerIdAndCompanyIdQuery(
        long CustomerId,
        long CompanyId) : IQuery<AsaasIntegrationCustomerResponse>;
}
