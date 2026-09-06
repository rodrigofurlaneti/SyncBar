namespace SyncBar.Application.Features.Integrations.Keeta.Order.Polling
{
    public sealed record PollKeetaEventsResponse(int TotalEvents, int ProcessedEvents, int UnprocessedEvents);
}
