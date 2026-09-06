using SyncBar.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Domain.Entities
{
    public sealed class KeetaIntegrationOrder : AggregateRoot
    {
        public long CompanyId { get; private set; }
        public long BranchId { get; private set; }
        public long CustomerId { get; private set; }
        public long CustomerOrderId { get; private set; }
        public string KeetaOrderId { get; private set; }
        public string DisplayId { get; private set; }
        public string InternalMerchantId { get; private set; }
        public long KeetaMerchantId { get; private set; }
        public string Status { get; private set; }
        public string OrderType { get; private set; }
        public string DeliveredBy { get; private set; }
        public decimal OrderAmount { get; private set; }
        public string Currency { get; private set; }
        public string RawOrderJson { get; private set; }
        public DateTime OrderCreatedAtUtc { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? ConfirmedAtUtc { get; private set; }
        public DateTime? ReadyForPickupAtUtc { get; private set; }
        public DateTime? ConcludedAtUtc { get; private set; }

        private KeetaIntegrationOrder() : base(0) { }

        private KeetaIntegrationOrder(long companyId, long branchId, long customerId, long customerOrderId, string keetaOrderId, string displayId, string internalMerchantId, long keetaMerchantId, string orderType, string deliveredBy, decimal orderAmount, string rawOrderJson, DateTime orderCreatedAtUtc) : base(0)
        {
            CompanyId = companyId;
            BranchId = branchId;
            CustomerId = customerId;
            CustomerOrderId = customerOrderId;
            KeetaOrderId = keetaOrderId;
            DisplayId = displayId;
            InternalMerchantId = internalMerchantId;
            KeetaMerchantId = keetaMerchantId;
            Status = "CREATED";
            OrderType = orderType;
            DeliveredBy = deliveredBy;
            OrderAmount = orderAmount;
            Currency = "BRL";
            RawOrderJson = rawOrderJson;
            OrderCreatedAtUtc = orderCreatedAtUtc;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public static Result<KeetaIntegrationOrder> Create(long companyId, long branchId, long customerId, long customerOrderId, string keetaOrderId, string displayId, string internalMerchantId, long keetaMerchantId, string orderType, string deliveredBy, decimal orderAmount, string rawOrderJson, DateTime orderCreatedAtUtc)
            => Result.Success(new KeetaIntegrationOrder(companyId, branchId, customerId, customerOrderId, keetaOrderId, displayId, internalMerchantId, keetaMerchantId, orderType, deliveredBy, orderAmount, rawOrderJson, orderCreatedAtUtc));

        public void ChangeStatus(string newStatus)
        {
            Status = newStatus;
        }

        public void MarkAsConfirmed()
        {
            Status = "CONFIRMED";
            ConfirmedAtUtc = DateTime.UtcNow;
        }

        public void MarkAsReadyForPickup()
        {
            Status = "READY_FOR_PICKUP";
            ReadyForPickupAtUtc = DateTime.UtcNow;
        }

        public void MarkAsConcluded()
        {
            Status = "CONCLUDED";
            ConcludedAtUtc = DateTime.UtcNow;
        }
    }
}
