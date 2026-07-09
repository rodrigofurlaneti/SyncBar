using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Users.Create;

public sealed record CreateUserCommand(
    long CompanyId,
    long? EmployeeId,
    string UserName,
    string Email,
    string Password,
    IReadOnlyCollection<long> RoleIds) : ICommand<long>;
