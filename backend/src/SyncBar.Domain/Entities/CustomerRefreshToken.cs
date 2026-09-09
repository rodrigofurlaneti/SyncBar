using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CustomerRefreshToken : Entity
{
    public long CustomerAppUserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CustomerRefreshToken() : base(0) { }

    private CustomerRefreshToken(long customerAppUserId, string token, DateTime expiresAt) : base(0)
    {
        CustomerAppUserId = customerAppUserId;
        Token = token;
        ExpiresAt = expiresAt;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<CustomerRefreshToken> Create(long customerAppUserId, string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<CustomerRefreshToken>(new Error("CustomerRefreshToken.EmptyToken", "Token is required."));
        if (expiresAt <= DateTime.Now)
            return Result.Failure<CustomerRefreshToken>(new Error("CustomerRefreshToken.InvalidExpiration", "Expiration must be in the future."));

        return Result.Success(new CustomerRefreshToken(customerAppUserId, token, expiresAt));
    }

    public bool IsValid() => RevokedAt is null && ExpiresAt > DateTime.Now && IsActive;

    public void Revoke()
    {
        RevokedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }
}
