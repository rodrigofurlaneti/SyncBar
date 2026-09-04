using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAppUser.GetByCompanyId
{
    public sealed record GetCustomerAppUsersByCompanyIdQuery(long CompanyId) : IQuery<IEnumerable<CustomerAppUserResponse>>;
}
