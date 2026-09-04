using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Storefront.GetBranchMenu
{
    public sealed record GetBranchMenuQuery(long BranchId) : IQuery<BranchMenuResponse>;
}
