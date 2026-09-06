using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.ProcessAuthorizationWebhook
{
    public sealed record ProcessKeetaAuthorizationWebhookCommand(string RawPayload, string? Signature) : ICommand;
}
