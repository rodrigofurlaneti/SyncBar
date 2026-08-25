using MediatR;
using SyncBar.Domain.Primitives;
namespace SyncBar.Application.Features.Dining.Messages.GetWaiterMessagesByBranch
{
    public sealed record GetWaiterMessagesByBranchQuery(long BranchId, long? DiningAreaId) : IRequest<Result<IEnumerable<WaiterMessageResponse>>>;
}
