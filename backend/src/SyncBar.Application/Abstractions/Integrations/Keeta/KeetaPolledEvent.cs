namespace SyncBar.Application.Abstractions.Integrations.Keeta
{
    /// <summary>Espelha os campos-base do schema "Event" da API de polling da Keeta.</summary>
    public sealed record KeetaPolledEvent(
        string EventId,
        string EventType,
        string OrderId,
        string OrderUrl,
        DateTime CreatedAtUtc,
        string RawJson);
}
