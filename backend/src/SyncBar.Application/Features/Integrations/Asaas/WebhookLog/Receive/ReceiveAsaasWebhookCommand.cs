using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Receive
{
    public sealed record ReceiveAsaasWebhookCommand(
        string RawPayload,
        string? AccessToken,
        string? IpAddress) : ICommand;
}
