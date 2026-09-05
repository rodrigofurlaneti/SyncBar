using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Delete
{
    public sealed record DeleteAsaasPaymentCommand(long PaymentId) : ICommand;
}
