using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AccessLog : Entity
{
    public long? AppUserId { get; private set; }
    public string UserName { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AccessLog() : base(0) { }

    private AccessLog(long? appUserId, string userName, string eventType, string? ipAddress, string? userAgent) : base(0)
    {
        AppUserId = appUserId;
        UserName = userName;
        EventType = eventType;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AccessLog> Create(long? appUserId, string userName, string eventType, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result.Failure<AccessLog>(new Error("AccessLog.EmptyUserName", "UserName is required."));
        if (string.IsNullOrWhiteSpace(eventType))
            return Result.Failure<AccessLog>(new Error("AccessLog.EmptyEventType", "EventType is required."));
        return Result.Success(new AccessLog(appUserId, userName, eventType, ipAddress, userAgent));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
