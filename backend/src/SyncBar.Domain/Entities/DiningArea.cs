using SyncBar.Domain.Primitives;
using System;
namespace SyncBar.Domain.Entities
{
    public sealed class DiningArea : AggregateRoot
    {
        public long BranchId { get; private set; }
        public string Name { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsActive { get; private set; }
        private DiningArea() : base(0) { }
        private DiningArea(long branchId, string name) : base(0)
        {
            BranchId = branchId;
            Name = name;
            IsActive = true;
            CreatedAt = DateTime.Now;
        }
        public static Result<DiningArea> Create(long branchId, string name)
        {
            if (branchId <= 0)
                return Result.Failure<DiningArea>(new Error("DiningArea.InvalidBranchId", "BranchId is required and must be greater than zero."));

            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<DiningArea>(new Error("DiningArea.EmptyName", "Name is required."));

            return Result.Success(new DiningArea(branchId, name));
        }
        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            Name = name;
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