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
    public string ZipCode { get; private set; } = null!;
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
        string? supplement,
        string zipCode) : base(0)
    {
        CompanyId = companyId;
        BranchId = branchId;
        CustomerId = customerId;
        Street = street;
        Number = number;
        Supplement = supplement ?? string.Empty;
        ZipCode = zipCode;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<CustomerAddress> Create(
        long companyId,
        long? branchId,
        long? customerId,
        string street,
        string number,
        string? supplement,
        string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Result.Failure<CustomerAddress>(new Error("CustomerAddress.EmptyStreet", "Street is required."));
        if (string.IsNullOrWhiteSpace(number))
            return Result.Failure<CustomerAddress>(new Error("CustomerAddress.EmptyNumber", "Number is required."));
        if (string.IsNullOrWhiteSpace(zipCode))
            return Result.Failure<CustomerAddress>(new Error("CustomerAddress.EmptyZipCode", "Zip code is required."));

        return Result.Success(new CustomerAddress(companyId, branchId, customerId, street, number, supplement, zipCode));
    }

    // Supplement (complemento) é o único campo de endereço genuinamente opcional — Street/Number/
    // ZipCode são exigidos acima. O parâmetro é `string?` porque isso reflete a nulabilidade real:
    // Nullable Reference Types é uma checagem só em tempo de compilação, então um `string`
    // "não anulável" ainda pode chegar null aqui vindo de desserialização JSON — daí o `?? string.Empty`
    // continuar existindo, agora sem ser um "unreachable code" para o analisador.
    public Result UpdateDetails(string street, string number, string? supplement, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Result.Failure(new Error("CustomerAddress.EmptyStreet", "Street is required."));
        if (string.IsNullOrWhiteSpace(number))
            return Result.Failure(new Error("CustomerAddress.EmptyNumber", "Number is required."));
        if (string.IsNullOrWhiteSpace(zipCode))
            return Result.Failure(new Error("CustomerAddress.EmptyZipCode", "Zip code is required."));

        Street = street;
        Number = number;
        Supplement = supplement ?? string.Empty;
        ZipCode = zipCode;
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
