using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Create
{
    public sealed record CreateAsaasIntegrationCustomerCommand(
        long CustomerId,
        long CompanyId,
        string AsaasCustomerId) : ICommand<long>;
}
