using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Update
{
    public sealed record UpdateAsaasIntegrationCustomerCommand(
        long Id,
        string NewAsaasCustomerId) : ICommand;
}
