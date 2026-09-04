using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Features.CustomerAppUser.Create;

public sealed record CreateCustomerAppUserCommand(
    long CompanyId,
    long? BranchId,
    long? CustomerId,
    string UserName,
    string Email,
    string Password,
    string? Phone = null
) : ICommand<long>;