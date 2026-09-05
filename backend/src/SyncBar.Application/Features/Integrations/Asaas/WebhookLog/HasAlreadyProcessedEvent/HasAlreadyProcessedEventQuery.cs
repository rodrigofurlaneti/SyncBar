using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent
{
    public sealed record HasAlreadyProcessedEventQuery(
        string AsaasEventId) : IQuery<bool>;
}
