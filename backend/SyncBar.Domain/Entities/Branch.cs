using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Branch : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Cnpj { get; private set; }
    public string? Phone { get; private set; }
    public string? AddressStreet { get; private set; }
    public string? AddressNumber { get; private set; }
    public string? AddressDistrict { get; private set; }
    public string? AddressCity { get; private set; }
    public string? AddressState { get; private set; }
    public string? AddressZipCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Branch() : base(0) { }

    private Branch(long companyId, string name, string? cnpj, string? phone, string? addressStreet, string? addressNumber, string? addressDistrict, string? addressCity, string? addressState, string? addressZipCode) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        Cnpj = cnpj;
        Phone = phone;
        AddressStreet = addressStreet;
        AddressNumber = addressNumber;
        AddressDistrict = addressDistrict;
        AddressCity = addressCity;
        AddressState = addressState;
        AddressZipCode = addressZipCode;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Branch> Create(long companyId, string name, string? cnpj, string? phone, string? addressStreet, string? addressNumber, string? addressDistrict, string? addressCity, string? addressState, string? addressZipCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Branch>(new Error("Branch.EmptyName", "Name is required."));
        return Result.Success(new Branch(companyId, name, cnpj, phone, addressStreet, addressNumber, addressDistrict, addressCity, addressState, addressZipCode));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
