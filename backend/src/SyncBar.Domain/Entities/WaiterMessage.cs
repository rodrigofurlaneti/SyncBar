using SyncBar.Domain.Primitives;
using System;

namespace SyncBar.Domain.Entities
{
    public sealed class WaiterMessage : AggregateRoot
    {
        public long BranchId { get; private set; }
        public long SenderEmployeeId { get; private set; }
        public long? RecipientEmployeeId { get; private set; }
        public long DiningAreaId { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public bool IsRead { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsActive { get; private set; }
        private WaiterMessage() : base(0) { }
        private WaiterMessage(long branchId, long senderEmployeeId, long? recipientEmployeeId, long diningAreaId, string message) : base(0)
        {
            BranchId = branchId;
            SenderEmployeeId = senderEmployeeId;
            RecipientEmployeeId = recipientEmployeeId;
            DiningAreaId = diningAreaId;
            Message = message;
            IsRead = false;
            IsActive = true;
            CreatedAt = DateTime.Now;
        }
        public static Result<WaiterMessage> Create(long branchId, long senderEmployeeId, long? recipientEmployeeId, long diningAreaId, string message)
        {
            if (branchId <= 0)
                return Result.Failure<WaiterMessage>(new Error("WaiterMessage.InvalidBranchId", "BranchId must be greater than zero."));
            if (senderEmployeeId <= 0)
                return Result.Failure<WaiterMessage>(new Error("WaiterMessage.InvalidSenderId", "SenderEmployeeId must be greater than zero."));
            if (diningAreaId <= 0)
                return Result.Failure<WaiterMessage>(new Error("WaiterMessage.InvalidDiningAreaId", "DiningAreaId cannot be null or zero."));
            if (string.IsNullOrWhiteSpace(message))
                return Result.Failure<WaiterMessage>(new Error("WaiterMessage.EmptyMessage", "Message content cannot be empty."));
            return Result.Success(new WaiterMessage(branchId, senderEmployeeId, recipientEmployeeId, diningAreaId, message.Trim()));
        }
        public void MarkAsRead()
        {
            IsRead = true;
            UpdatedAt = DateTime.Now;
        }
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.Now;
        }
    }
}

