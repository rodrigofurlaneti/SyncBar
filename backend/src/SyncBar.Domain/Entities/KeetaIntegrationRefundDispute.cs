using SyncBar.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Domain.Entities
{
    public sealed class KeetaIntegrationRefundDispute : AggregateRoot
    {
        public long CompanyId { get; private set; }
        public long BranchId { get; private set; }
        public string OrderId { get; private set; } = null!;
        public long AfterSaleOrderId { get; private set; }
        public decimal RefundAmount { get; private set; }
        public string Currency { get; private set; } = null!;
        public string ApplyReason { get; private set; } = null!;
        public string ResolutionStatus { get; private set; } = null!;
        public string? DenialReasonCode { get; private set; }
        public string? DenialReasonText { get; private set; }
        public DateTime ReceivedAtUtc { get; private set; }
        public DateTime? ResolvedAtUtc { get; private set; }

        private KeetaIntegrationRefundDispute() : base(0) { }

        private KeetaIntegrationRefundDispute(long companyId, long branchId, string orderId, long afterSaleOrderId, decimal refundAmount, string applyReason) : base(0)
        {
            CompanyId = companyId;
            BranchId = branchId;
            OrderId = orderId;
            AfterSaleOrderId = afterSaleOrderId;
            RefundAmount = refundAmount;
            Currency = "BRL";
            ApplyReason = applyReason;
            ResolutionStatus = "PENDING";
            ReceivedAtUtc = DateTime.UtcNow;
        }

        public static Result<KeetaIntegrationRefundDispute> Create(long companyId, long branchId, string orderId, long afterSaleOrderId, decimal refundAmount, string applyReason)
            => Result.Success(new KeetaIntegrationRefundDispute(companyId, branchId, orderId, afterSaleOrderId, refundAmount, applyReason));

        public void Resolve(bool accepted, string? denialReasonCode = null, string? denialReasonText = null)
        {
            ResolutionStatus = accepted ? "ACCEPTED" : "REJECTED";

            if (!accepted)
            {
                DenialReasonCode = denialReasonCode;
                DenialReasonText = denialReasonText;
            }

            ResolvedAtUtc = DateTime.UtcNow;
        }
    }
}
