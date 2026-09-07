using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.OrderOrigin.GetAll;
namespace SyncBar.Application.Features.OrderOrigin.GetByCompanyAndBranch
{
    public sealed record GetOrderOriginsByCompanyAndBranchQuery(
        long? CompanyId,
        long? BranchId) : IQuery<IReadOnlyCollection<OrderOriginResponse>>;
}
