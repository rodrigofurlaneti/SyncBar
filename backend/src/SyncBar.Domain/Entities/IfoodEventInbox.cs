using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class IfoodEventInbox : Entity
{
    private IfoodEventInbox() : base(0) { }
    public long CompanyId { get; private set; }
    public string EventId { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public static IfoodEventInbox Receive(long companyId, string eventId, string payload, DateTime now) =>
        new() { CompanyId = companyId, EventId = eventId, Payload = payload, ReceivedAtUtc = now, NextAttemptAtUtc = now };
}
