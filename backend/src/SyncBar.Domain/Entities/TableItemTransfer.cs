using SyncBar.Domain.Primitives;
using System;
namespace SyncBar.Domain.Entities
{
    public sealed class TableItemTransfer : AggregateRoot
    {
        public long CustomerOrderId { get; private set; }
        public long CustomerOrderItemId { get; private set; }
        public long SourceDiningTableId { get; private set; }
        public long TargetDiningTableId { get; private set; }
        public long EmployeeId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsActive { get; private set; }
        private TableItemTransfer() : base(0) { }
        private TableItemTransfer(
            long customerOrderId,
            long customerOrderItemId,
            long sourceDiningTableId,
            long targetDiningTableId,
            long employeeId) : base(0)
        {
            CustomerOrderId = customerOrderId;
            CustomerOrderItemId = customerOrderItemId;
            SourceDiningTableId = sourceDiningTableId;
            TargetDiningTableId = targetDiningTableId;
            EmployeeId = employeeId;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }
        public static Result<TableItemTransfer> Create(
            long customerOrderId,
            long customerOrderItemId,
            long sourceDiningTableId,
            long targetDiningTableId,
            long employeeId)
        {
            if (customerOrderId <= 0)
                return Result.Failure<TableItemTransfer>(new Error("TableItemTransfer.InvalidOrder", "Order ID must be valid."));
            if (customerOrderItemId <= 0)
                return Result.Failure<TableItemTransfer>(new Error("TableItemTransfer.InvalidItem", "Item ID must be valid."));
            if (sourceDiningTableId == targetDiningTableId)
                return Result.Failure<TableItemTransfer>(new Error("TableItemTransfer.SameTable", "Source and target tables cannot be the same."));
            if (employeeId <= 0)
                return Result.Failure<TableItemTransfer>(new Error("TableItemTransfer.InvalidEmployee", "Employee ID must be valid."));
            return Result.Success(new TableItemTransfer(
                customerOrderId,
                customerOrderItemId,
                sourceDiningTableId,
                targetDiningTableId,
                employeeId));
        }
    }
}

