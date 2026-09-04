using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CustomerAppUser : AggregateRoot
{
    private const int MaxFailedAccessAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public long CompanyId { get; private set; }
    public long? BranchId { get; private set; }
    public long? CustomerId { get; private set; }
    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public int FailedAccessCount { get; private set; }
    public DateTime? LockoutEndAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CustomerAppUser() : base(0) { }

    private CustomerAppUser(long companyId, long? branchId, long? customerId, string userName, string email, string passwordHash) : base(0)
    {
        CompanyId = companyId;
        BranchId = branchId;
        CustomerId = customerId;
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
        FailedAccessCount = 0;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<CustomerAppUser> Create(long companyId, long? branchId, long? customerId, string userName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result.Failure<CustomerAppUser>(new Error("CustomerAppUser.EmptyUserName", "UserName is required."));
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<CustomerAppUser>(new Error("CustomerAppUser.EmptyEmail", "Email is required."));
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<CustomerAppUser>(new Error("CustomerAppUser.EmptyPasswordHash", "Password hash is required."));

        return Result.Success(new CustomerAppUser(companyId, branchId, customerId, userName, email, passwordHash));
    }

    public bool IsLockedOut() => LockoutEndAt.HasValue && LockoutEndAt.Value > DateTime.Now;

    public void RegisterLoginFailure()
    {
        FailedAccessCount++;
        if (FailedAccessCount >= MaxFailedAccessAttempts)
            LockoutEndAt = DateTime.Now.Add(LockoutDuration);
        UpdatedAt = DateTime.Now;
    }

    public void RegisterLoginSuccess()
    {
        FailedAccessCount = 0;
        LockoutEndAt = null;
        LastLoginAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    public Result ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure(new Error("CustomerAppUser.EmptyPasswordHash", "Password hash is required."));

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }
    public void UpdateDetails(long companyId, long? branchId, long? customerId, string userName, string email)
    {
        CompanyId = companyId;
        BranchId = branchId;
        CustomerId = customerId;
        UserName = userName;
        Email = email;
        UpdatedAt = DateTime.Now;
    }
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}