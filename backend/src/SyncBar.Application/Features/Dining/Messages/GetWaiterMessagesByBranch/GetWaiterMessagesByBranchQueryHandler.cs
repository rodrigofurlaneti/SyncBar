using MediatR;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Dining.Messages.GetWaiterMessagesByBranch
{
    internal sealed class GetWaiterMessagesByBranchQueryHandler(
    IWaiterMessageRepository messageRepository)
    : IRequestHandler<GetWaiterMessagesByBranchQuery, Result<IEnumerable<WaiterMessageResponse>>>
    {
        public async Task<Result<IEnumerable<WaiterMessageResponse>>> Handle(
            GetWaiterMessagesByBranchQuery request,
            CancellationToken cancellationToken)
        {
            var messages = await messageRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
            if (request.DiningAreaId.HasValue)
            {
                messages = messages.Where(m => m.DiningAreaId == request.DiningAreaId.Value);
            }
            var response = messages.Select(m => new WaiterMessageResponse(
                m.Id,
                m.BranchId,
                m.SenderEmployeeId,
                m.RecipientEmployeeId,
                m.DiningAreaId,
                m.Message,
                m.IsRead,
                m.CreatedAt.ToString("o")
            ));

            return Result.Success<IEnumerable<WaiterMessageResponse>>(response);
        }
    }
}
