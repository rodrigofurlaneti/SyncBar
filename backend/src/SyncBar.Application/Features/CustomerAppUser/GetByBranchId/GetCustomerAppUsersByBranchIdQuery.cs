using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAppUser.GetByBranchId
{
    public sealed record GetCustomerAppUsersByBranchIdQuery(long BranchId) : IQuery<IEnumerable<CustomerAppUserResponse>>;
}
