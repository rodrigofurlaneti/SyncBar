namespace SyncBar.Application.Abstractions.Integrations.Keeta
{
    public interface IKeetaAuthClient
    {
        Task<string> GetAuthorizationUrlAsync(long companyId, long branchId, string redirectUri, CancellationToken cancellationToken = default);

        Task<KeetaTokenResponse> GetAccessTokenAsync(long companyId, long branchId, CancellationToken cancellationToken = default);

        Task<KeetaMerchantInfoResponse> GetMerchantInfoAsync(
            long companyId,
            long branchId,
            string authId,
            string accessToken,
            int pageNum = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);
    }
}
