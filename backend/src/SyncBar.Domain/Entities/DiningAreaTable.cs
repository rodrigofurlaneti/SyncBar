using SyncBar.Domain.Primitives;
using System;
using System.Xml.Linq;
namespace SyncBar.Domain.Entities
{
    public sealed class DiningAreaTable : AggregateRoot
    {
        public long DiningAreaId { get; private set; }
        public long DiningTableId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsActive { get; private set; }
        private DiningAreaTable() : base(0) { }
        private DiningAreaTable(long diningAreaId, long diningTableId) : base(0)
        {
            DiningAreaId = diningAreaId;
            DiningTableId = diningTableId;
            IsActive = true;
            CreatedAt = DateTime.Now;
        }
        public static Result<DiningAreaTable> Create(long diningAreaId, long diningTableId)
        {
            if (diningAreaId <= 0)
                return Result.Failure<DiningAreaTable>(new Error("DiningAreaTable.InvalidDiningAreaId", "DiningAreaId must be greater than zero."));

            if (diningTableId <= 0)
                return Result.Failure<DiningAreaTable>(new Error("DiningAreaTable.InvalidDiningTableId", "DiningTableId must be greater than zero."));

            return Result.Success(new DiningAreaTable(diningAreaId, diningTableId));
        }
        public void UpdateAssignment(long diningAreaId, long diningTableId)
        {
            if (diningAreaId > 0)
                DiningAreaId = diningAreaId;
            if (diningTableId > 0)
                DiningTableId = diningTableId;
            UpdatedAt = DateTime.Now;
        }
        public void Touch() => UpdatedAt = DateTime.Now;
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.Now;
        }
    }
}