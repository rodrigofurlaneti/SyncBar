using SyncBar.Domain.Primitives;
using System;

namespace SyncBar.Domain.Entities;

public sealed class DiningAreaAssignment : AggregateRoot
{
    public long DiningAreaId { get; private set; }
    public long EmployeeId { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime? EndAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    private DiningAreaAssignment() : base(0) { }
    private DiningAreaAssignment(long diningAreaId, long employeeId, DateTime startAt) : base(0)
    {
        DiningAreaId = diningAreaId;
        EmployeeId = employeeId;
        StartAt = startAt;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }
    public static Result<DiningAreaAssignment> Create(long diningAreaId, long employeeId, DateTime startAt)
    {
        if (diningAreaId <= 0)
            return Result.Failure<DiningAreaAssignment>(new Error("DiningAreaAssignment.InvalidDiningAreaId", "DiningAreaId must be greater than zero."));
        if (employeeId <= 0)
            return Result.Failure<DiningAreaAssignment>(new Error("DiningAreaAssignment.InvalidEmployeeId", "EmployeeId must be greater than zero."));
        if (startAt == default)
            return Result.Failure<DiningAreaAssignment>(new Error("DiningAreaAssignment.InvalidStartAt", "StartAt must be a valid date and time."));
        return Result.Success(new DiningAreaAssignment(diningAreaId, employeeId, startAt));
    }
    public void EndAssignment(DateTime endAt)
    {
        EndAt = endAt;
        UpdatedAt = DateTime.Now;
    }
    public void Touch() => UpdatedAt = DateTime.Now;
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}