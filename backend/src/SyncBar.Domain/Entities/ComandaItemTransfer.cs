using SyncBar.Domain.Primitives;
using System;
namespace SyncBar.Domain.Entities
{
    public sealed class ComandaItemTransfer : AggregateRoot
    {
        public long CustomerOrderId { get; private set; }
        public long CustomerOrderItemId { get; private set; }
        public long SourceComandaId { get; private set; }
        public long TargetComandaId { get; private set; }
        public long EmployeeId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsActive { get; private set; }

        private ComandaItemTransfer() : base(0) { }

        private ComandaItemTransfer(
            long customerOrderId,
            long customerOrderItemId,
            long sourceComandaId,
            long targetComandaId,
            long employeeId) : base(0)
        {
            CustomerOrderId = customerOrderId;
            CustomerOrderItemId = customerOrderItemId;
            SourceComandaId = sourceComandaId;
            TargetComandaId = targetComandaId;
            EmployeeId = employeeId;
            CreatedAt = DateTime.Now;
            IsActive = true;
        }

        public static Result<ComandaItemTransfer> Create(
            long customerOrderId,
            long customerOrderItemId,
            long sourceComandaId,
            long targetComandaId,
            long employeeId)
        {
            if (customerOrderId <= 0)
                return Result.Failure<ComandaItemTransfer>(new Error("ComandaItemTransfer.InvalidOrder", "Order ID must be valid."));
            if (customerOrderItemId <= 0)
                return Result.Failure<ComandaItemTransfer>(new Error("ComandaItemTransfer.InvalidItem", "Item ID must be valid."));
            if (sourceComandaId == targetComandaId)
                return Result.Failure<ComandaItemTransfer>(new Error("ComandaItemTransfer.SameComanda", "Source and target comandas cannot be the same."));
            if (employeeId <= 0)
                return Result.Failure<ComandaItemTransfer>(new Error("ComandaItemTransfer.InvalidEmployee", "Employee ID must be valid."));

            return Result.Success(new ComandaItemTransfer(
                customerOrderId,
                customerOrderItemId,
                sourceComandaId,
                targetComandaId,
                employeeId));
        }
    }
}
