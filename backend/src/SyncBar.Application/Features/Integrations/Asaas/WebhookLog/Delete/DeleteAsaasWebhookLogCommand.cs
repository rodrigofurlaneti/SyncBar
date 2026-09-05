using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Delete
{
    public sealed record DeleteAsaasWebhookLogCommand(
        long Id,
        long CompanyId) : ICommand;
}
