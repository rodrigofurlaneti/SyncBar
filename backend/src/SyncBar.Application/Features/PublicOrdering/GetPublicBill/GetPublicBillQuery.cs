using MediatR;
using SyncBar.Domain.Primitives;
namespace SyncBar.Application.Features.PublicOrdering.GetPublicBill
{
    public sealed record GetPublicBillQuery(Guid Token) : IRequest<Result<PublicBillResponse>>;
}
