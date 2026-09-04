using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.CustomerAppUser.GetById;
namespace SyncBar.Application.Features.CustomerAppUser.GetByCustomerId
{
    public sealed record GetCustomerAppUsersByCustomerIdQuery(long CustomerId) : IQuery<IEnumerable<CustomerAppUserResponse>>;
}
