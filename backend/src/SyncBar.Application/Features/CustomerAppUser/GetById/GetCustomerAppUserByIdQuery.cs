using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAppUser.GetById
{
    public sealed record GetCustomerAppUserByIdQuery(long Id) : IQuery<CustomerAppUserResponse>;
}
