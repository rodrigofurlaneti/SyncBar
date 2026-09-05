using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetById
{
    public sealed record GetAsaasCustomerByIdQuery(
        long Id) : IQuery<AsaasIntegrationCustomerResponse>;
}
