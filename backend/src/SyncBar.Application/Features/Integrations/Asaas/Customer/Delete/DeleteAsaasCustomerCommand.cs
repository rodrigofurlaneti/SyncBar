using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Delete
{
    public sealed record DeleteAsaasCustomerCommand(long CustomerId, long CompanyId) : ICommand;
}
