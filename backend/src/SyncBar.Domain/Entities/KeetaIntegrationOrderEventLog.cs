using SyncBar.Domain.Primitives;
namespace SyncBar.Domain.Entities
{
    public sealed class KeetaIntegrationOrderEventLog : AggregateRoot
    {
        public long CompanyId { get; private set; }
        public long BranchId { get; private set; }
        public string EventId { get; private set; } = null!;
        public string OrderId { get; private set; } = null!;
        public string EventType { get; private set; } = null!;
        public string? RawPayload { get; private set; }
        public bool ProcessedSuccessfully { get; private set; }
        public string? ErrorMessage { get; private set; }
        public DateTime EventCreatedAtUtc { get; private set; }
        public DateTime ReceivedAtUtc { get; private set; }

        private KeetaIntegrationOrderEventLog() : base(0) { }

        private KeetaIntegrationOrderEventLog(long companyId, long branchId, string eventId, string orderId, string eventType, string? rawPayload, DateTime eventCreatedAtUtc) : base(0)
        {
            CompanyId = companyId;
            BranchId = branchId;
            EventId = eventId;
            OrderId = orderId;
            EventType = eventType;
            RawPayload = rawPayload;
            EventCreatedAtUtc = eventCreatedAtUtc;
            ProcessedSuccessfully = false;
            ReceivedAtUtc = DateTime.UtcNow;
        }

        public static Result<KeetaIntegrationOrderEventLog> Create(long companyId, long branchId, string eventId, string orderId, string eventType, string? rawPayload, DateTime eventCreatedAtUtc)
            => Result.Success(new KeetaIntegrationOrderEventLog(companyId, branchId, eventId, orderId, eventType, rawPayload, eventCreatedAtUtc));

        public void MarkAsProcessed()
        {
            ProcessedSuccessfully = true;
            ErrorMessage = null;
        }

        public void MarkAsFailed(string errorMessage)
        {
            ProcessedSuccessfully = false;
            ErrorMessage = errorMessage;
        }
    }
}
