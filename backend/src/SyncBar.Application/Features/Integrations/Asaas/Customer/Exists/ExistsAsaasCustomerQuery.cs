using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Exists
{
    public sealed record ExistsAsaasCustomerQuery(
        long CustomerId,
        long CompanyId) : IQuery<bool>;
}
