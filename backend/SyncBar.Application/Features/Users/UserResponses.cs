namespace SyncBar.Application.Features.Users;

public sealed record UserResponse(
    long Id,
    string UserName,
    string Email,
    long? EmployeeId,
    bool IsActive,
    IReadOnlyCollection<long> RoleIds);

public sealed record RoleResponse(long Id, string Name, string? Description);
