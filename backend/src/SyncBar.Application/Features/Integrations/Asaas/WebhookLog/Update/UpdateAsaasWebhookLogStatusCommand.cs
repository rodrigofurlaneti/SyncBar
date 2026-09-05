using SyncBar.Domain.Enums;
using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Update
{
    public sealed record UpdateAsaasWebhookLogStatusCommand(
        long Id,
        long CompanyId,
        WebhookLogStatus Status,
        string? ErrorMessage = null) : ICommand;
}
