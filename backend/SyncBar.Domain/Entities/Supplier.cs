using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Supplier : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string LegalName { get; private set; } = null!;
    public string? TradeName { get; private set; }
    public string? Cnpj { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Supplier() : base(0) { }

    private Supplier(long companyId, string legalName, string? tradeName, string? cnpj, string? email, string? phone) : base(0)
    {
        CompanyId = companyId;
        LegalName = legalName;
        TradeName = tradeName;
        Cnpj = cnpj;
        Email = email;
        Phone = phone;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Supplier> Create(long companyId, string legalName, string? tradeName, string? cnpj, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            return Result.Failure<Supplier>(new Error("Supplier.EmptyLegalName", "LegalName is required."));
        return Result.Success(new Supplier(companyId, legalName, tradeName, cnpj, email, phone));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
