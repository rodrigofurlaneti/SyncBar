using SyncBar.Domain.Primitives;
namespace SyncBar.Domain.Entities
{
    public sealed class KeetaIntegrationMerchantMapping : AggregateRoot
    {
        public long CompanyId { get; private set; }
        public long BranchId { get; private set; }
        public string InternalMerchantId { get; private set; }
        public long KeetaMerchantId { get; private set; }
        public string StoreName { get; private set; }
        public string? TimeZone { get; private set; }
        public bool IsAuthorized { get; private set; }
        public bool IsOnboarded { get; private set; }
        public DateTime? LastMenuSyncAtUtc { get; private set; }
        public string? MenuBaseUrl { get; private set; }
        public string? WebhookUrl { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        private KeetaIntegrationMerchantMapping() : base(0) { }

        private KeetaIntegrationMerchantMapping(long companyId, long branchId, string internalMerchantId, long keetaMerchantId, string storeName) : base(0)
        {
            CompanyId = companyId;
            BranchId = branchId;
            InternalMerchantId = internalMerchantId;
            KeetaMerchantId = keetaMerchantId;
            StoreName = storeName;
            IsAuthorized = true;
            IsOnboarded = false;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public static Result<KeetaIntegrationMerchantMapping> Create(long companyId, long branchId, string internalMerchantId, long keetaMerchantId, string storeName)
            => Result.Success(new KeetaIntegrationMerchantMapping(companyId, branchId, internalMerchantId, keetaMerchantId, storeName));

        public void UpdateStatus(bool isAuthorized, bool isOnboarded)
        {
            IsAuthorized = isAuthorized;
            IsOnboarded = isOnboarded;
        }

        public void UpdateUrls(string menuBaseUrl, string webhookUrl)
        {
            MenuBaseUrl = menuBaseUrl;
            WebhookUrl = webhookUrl;
        }

        public void RegisterMenuSync()
        {
            LastMenuSyncAtUtc = DateTime.UtcNow;
        }
    }
}
