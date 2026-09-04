using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CustomerAddress : AggregateRoot
{
    public long CompanyId { get; private set; }
    public long? BranchId { get; private set; }
    public long? CustomerId { get; private set; }
    public long? LastOrderId { get; private set; }
    public string Street { get; private set; } = null!;
    public string Number { get; private set; } = null!;
    public string Supplement { get; private set; } = null!;
    public DateTime? LastOrderAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CustomerAddress() : base(0) { }

    private CustomerAddress(
        long companyId,
        long? branchId,
        long? customerId,
        string street,
        string number,
        string supplement) : base(0)
    {
        CompanyId = companyId;
        BranchId = branchId;
        CustomerId = customerId;
        Street = street;
        Number = number;
        Supplement = supplement ?? string.Empty;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<CustomerAddress> Create(
        long companyId,
        long? branchId,
        long? customerId,
        string street,
        string number,
        string supplement)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Result.Failure<CustomerAddress>(new Error("CustomerAddress.EmptyStreet", "Street is required."));
        if (string.IsNullOrWhiteSpace(number))
            return Result.Failure<CustomerAddress>(new Error("CustomerAddress.EmptyNumber", "Number is required."));

        return Result.Success(new CustomerAddress(companyId, branchId, customerId, street, number, supplement));
    }

    public Result UpdateDetails(string street, string number, string supplement)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Result.Failure(new Error("CustomerAddress.EmptyStreet", "Street is required."));
        if (string.IsNullOrWhiteSpace(number))
            return Result.Failure(new Error("CustomerAddress.EmptyNumber", "Number is required."));

        Street = street;
        Number = number;
        Supplement = supplement ?? string.Empty;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public void RegisterOrderUsage(long orderId)
    {
        LastOrderId = orderId;
        LastOrderAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}