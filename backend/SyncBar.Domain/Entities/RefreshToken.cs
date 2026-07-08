using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class RefreshToken : Entity
{
    public long AppUserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private RefreshToken() : base(0) { }

    private RefreshToken(long appUserId, string token, DateTime expiresAt) : base(0)
    {
        AppUserId = appUserId;
        Token = token;
        ExpiresAt = expiresAt;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<RefreshToken> Create(long appUserId, string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<RefreshToken>(new Error("RefreshToken.EmptyToken", "Token is required."));
        if (expiresAt <= DateTime.UtcNow)
            return Result.Failure<RefreshToken>(new Error("RefreshToken.InvalidExpiration", "Expiration must be in the future."));

        return Result.Success(new RefreshToken(appUserId, token, expiresAt));
    }

    public bool IsValid() => RevokedAt is null && ExpiresAt > DateTime.UtcNow && IsActive;

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
