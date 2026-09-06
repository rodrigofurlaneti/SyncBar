using SyncBar.Domain.Primitives;
namespace SyncBar.Domain.Entities
{
    public sealed class KeetaIntegrationSetting : AggregateRoot
    {
        public long CompanyId { get; private set; }
        public long BranchId { get; private set; }
        public string? ClientId { get; private set; }
        public string? ClientSecret { get; private set; }
        public string? AppId { get; private set; }
        public string BaseUrl { get; private set; }
        public string? CurrentAccessToken { get; private set; }
        public DateTime? TokenExpiresAtUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }

        private KeetaIntegrationSetting() : base(0) { }

        private KeetaIntegrationSetting(long companyId, long branchId) : base(0)
        {
            CompanyId = companyId;
            BranchId = branchId;
            BaseUrl = "https://open.mykeeta.com/api/open/opendelivery";
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public static Result<KeetaIntegrationSetting> Create(long companyId, long branchId)
            => Result.Success(new KeetaIntegrationSetting(companyId, branchId));

        public Result SaveCredentials(string clientId, string clientSecret, string appId, string? baseUrl = null)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            AppId = appId;

            if (!string.IsNullOrWhiteSpace(baseUrl))
                BaseUrl = baseUrl;

            UpdatedAtUtc = DateTime.UtcNow;
            return Result.Success();
        }

        public void UpdateToken(string accessToken, DateTime expiresAtUtc)
        {
            CurrentAccessToken = accessToken;
            TokenExpiresAtUtc = expiresAtUtc;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
