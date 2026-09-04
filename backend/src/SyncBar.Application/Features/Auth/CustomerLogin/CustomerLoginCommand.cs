using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Auth.CustomerLogin
{
    public sealed record CustomerLoginCommand(
        string Email,
        string Password,
        int CompanyId,
        int? BranchId,
        string? IpAddress = null,
        string? UserAgent = null
    ) : ICommand<CustomerLoginResponse>;
}

