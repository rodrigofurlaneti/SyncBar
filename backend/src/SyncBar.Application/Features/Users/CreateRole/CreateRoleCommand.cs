using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Users.CreateRole;

public sealed record CreateRoleCommand(
    long CompanyId,
    string Name,
    string? Description) : ICommand<long>;
