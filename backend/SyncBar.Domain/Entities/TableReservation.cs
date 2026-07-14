using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class TableReservation : AggregateRoot
{
    public long BranchId { get; private set; }
    public long? DiningTableId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string? CustomerPhone { get; private set; }
    public int PartySize { get; private set; }
    public DateTime ReservedFor { get; private set; }
    public long ReservationStatusId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private TableReservation() : base(0) { }

    private TableReservation(long branchId, long? diningTableId, string customerName, string? customerPhone, int partySize, DateTime reservedFor, string? notes) : base(0)
    {
        BranchId = branchId;
        DiningTableId = diningTableId;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        PartySize = partySize;
        ReservedFor = reservedFor;
        Notes = notes;
        ReservationStatusId = ReservationStatusIds.Pending;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<TableReservation> Create(
        long branchId, long? diningTableId, string customerName, string? customerPhone,
        int partySize, DateTime reservedFor, string? notes)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return Result.Failure<TableReservation>(new Error("TableReservation.EmptyCustomerName", "Customer name is required."));
        if (partySize <= 0)
            return Result.Failure<TableReservation>(new Error("TableReservation.InvalidPartySize", "Party size must be greater than zero."));
        if (reservedFor <= DateTime.UtcNow)
            return Result.Failure<TableReservation>(new Error("TableReservation.PastDate", "Reservation date must be in the future."));

        return Result.Success(new TableReservation(branchId, diningTableId, customerName, customerPhone, partySize, reservedFor, notes));
    }

    public Result Confirm(long diningTableId)
    {
        if (ReservationStatusId != ReservationStatusIds.Pending)
            return Result.Failure(new Error("TableReservation.NotPending", "Only a pending reservation can be confirmed."));

        DiningTableId = diningTableId;
        ReservationStatusId = ReservationStatusIds.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkSeated()
    {
        if (ReservationStatusId != ReservationStatusIds.Confirmed)
            return Result.Failure(new Error("TableReservation.NotConfirmed", "Only a confirmed reservation can be seated."));

        ReservationStatusId = ReservationStatusIds.Seated;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (ReservationStatusId is ReservationStatusIds.Seated or ReservationStatusIds.Cancelled)
            return Result.Failure(new Error("TableReservation.CannotCancel", "This reservation can no longer be cancelled."));

        ReservationStatusId = ReservationStatusIds.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkNoShow()
    {
        if (ReservationStatusId != ReservationStatusIds.Confirmed)
            return Result.Failure(new Error("TableReservation.NotConfirmed", "Only a confirmed reservation can be marked as no-show."));

        ReservationStatusId = ReservationStatusIds.NoShow;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
