using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.CustomerAppUser.Update
{
    public sealed record UpdateCustomerAppUserCommand(
        long Id,
        long CompanyId,
        long? BranchId,
        long? CustomerId,
        string UserName,
        string Email,
        string? Password
    ) : ICommand;
}