using SyncBar.Domain.Primitives;
namespace SyncBar.Domain.Entities
{
    public sealed class KeetaIntegrationAuthorizationSession : AggregateRoot
    {
        public long CompanyId { get; private set; }
        public long BranchId { get; private set; }
        public string AuthId { get; private set; }
        public string? State { get; private set; }
        public long? KeetaMerchantId { get; private set; }
        public string? AuthorizationCode { get; private set; }
        public int OperationType { get; private set; }
        public bool IsProcessed { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        private KeetaIntegrationAuthorizationSession() : base(0) { }

        private KeetaIntegrationAuthorizationSession(long companyId, long branchId, string authId, int operationType) : base(0)
        {
            CompanyId = companyId;
            BranchId = branchId;
            AuthId = authId;
            OperationType = operationType;
            IsProcessed = false;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public static Result<KeetaIntegrationAuthorizationSession> Create(long companyId, long branchId, string authId, int operationType)
            => Result.Success(new KeetaIntegrationAuthorizationSession(companyId, branchId, authId, operationType));

        public void SetDetails(long? keetaMerchantId, string? authorizationCode, string? state)
        {
            KeetaMerchantId = keetaMerchantId;
            AuthorizationCode = authorizationCode;
            State = state;
        }

        public void MarkAsProcessed()
        {
            IsProcessed = true;
        }
    }
}
