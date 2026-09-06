namespace SyncBar.Application.Abstractions.Integrations.Keeta
{
    public sealed record KeetaMerchantInfoResponse(
        long UserId,
        long BrandId,
        string BrandName,
        IReadOnlyList<KeetaAuthorizedShop> AuthorizedShops);
}
