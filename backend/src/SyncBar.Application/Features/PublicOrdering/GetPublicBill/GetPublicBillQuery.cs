using MediatR;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
namespace SyncBar.Application.Features.PublicOrdering.GetPublicBill
{
    public sealed record GetPublicBillQuery(Guid Token) : IQuery<PublicBillResponse>;
}
