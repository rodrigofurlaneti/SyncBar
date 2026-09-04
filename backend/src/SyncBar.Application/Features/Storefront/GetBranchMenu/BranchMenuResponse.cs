using SyncBar.Application.Features.Catalog;
namespace SyncBar.Application.Features.Storefront.GetBranchMenu
{
    public sealed record BranchMenuResponse(
        string BranchName,
        List<MenuItemResponse> Items);
}
