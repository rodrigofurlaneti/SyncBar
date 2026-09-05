using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId
{
    public sealed record GetByAsaasCustomerIdQuery(
        string AsaasCustomerId) : IQuery<AsaasIntegrationCustomerResponse>;
}
