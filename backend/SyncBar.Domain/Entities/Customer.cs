using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Customer : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Cpf { get; private set; }
    public string? Email { get; private set; }
    public int LoyaltyPoints { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Customer() : base(0) { }

    private Customer(long companyId, string name, string? phone, string? cpf, string? email) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        Phone = phone;
        Cpf = cpf;
        Email = email;
        LoyaltyPoints = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Customer> Create(long companyId, string name, string? phone, string? cpf, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Customer>(new Error("Customer.EmptyName", "Name is required."));

        return Result.Success(new Customer(companyId, name, phone, cpf, email));
    }

    public Result UpdateDetails(string name, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("Customer.EmptyName", "Name is required."));

        Name = name;
        Phone = phone;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // Regra simples de fidelidade: 1 ponto a cada R$ 1 gasto — o chamador decide quantos
    // pontos conceder (ex.: RegisterSale pode chamar isso com Math.Floor(sale.TotalAmount)).
    public Result AddLoyaltyPoints(int points)
    {
        if (points <= 0)
            return Result.Failure(new Error("Customer.InvalidPoints", "Points must be greater than zero."));

        LoyaltyPoints += points;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RedeemPoints(int points)
    {
        if (points <= 0)
            return Result.Failure(new Error("Customer.InvalidPoints", "Points must be greater than zero."));
        if (points > LoyaltyPoints)
            return Result.Failure(new Error("Customer.InsufficientPoints", "Not enough loyalty points."));

        LoyaltyPoints -= points;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
