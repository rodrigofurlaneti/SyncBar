using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.OrderOrigin.GetAll;
namespace SyncBar.Application.Features.OrderOrigin.GetFiltered
{
    public sealed record GetFilteredOrderOriginsQuery(
        long? CompanyId,
        long? BranchId,
        string? SearchTerm,
        bool? IsActive) : IQuery<IReadOnlyCollection<OrderOriginResponse>>;
}
