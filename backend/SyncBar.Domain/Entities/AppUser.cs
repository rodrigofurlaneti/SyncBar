using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AppUser : AggregateRoot
{
    private const int MaxFailedAccessAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public long CompanyId { get; private set; }
    public long? EmployeeId { get; private set; }
    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? PasswordSalt { get; private set; }
    public int FailedAccessCount { get; private set; }
    public DateTime? LockoutEndAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AppUser() : base(0) { }

    private AppUser(long companyId, long? employeeId, string userName, string email, string passwordHash) : base(0)
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
        FailedAccessCount = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AppUser> Create(long companyId, long? employeeId, string userName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result.Failure<AppUser>(new Error("AppUser.EmptyUserName", "UserName is required."));
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<AppUser>(new Error("AppUser.EmptyEmail", "Email is required."));
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<AppUser>(new Error("AppUser.EmptyPasswordHash", "Password hash is required."));

        return Result.Success(new AppUser(companyId, employeeId, userName, email, passwordHash));
    }

    public bool IsLockedOut() => LockoutEndAt.HasValue && LockoutEndAt.Value > DateTime.UtcNow;

    public void RegisterLoginFailure()
    {
        FailedAccessCount++;
        if (FailedAccessCount >= MaxFailedAccessAttempts)
            LockoutEndAt = DateTime.UtcNow.Add(LockoutDuration);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RegisterLoginSuccess()
    {
        FailedAccessCount = 0;
        LockoutEndAt = null;
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Result ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure(new Error("AppUser.EmptyPasswordHash", "Password hash is required."));

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
