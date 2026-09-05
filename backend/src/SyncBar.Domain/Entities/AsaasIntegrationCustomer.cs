using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AsaasIntegrationCustomer : AggregateRoot
{
    public long CustomerId { get; private set; }
    public long CompanyId { get; private set; }
    public string AsaasCustomerId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AsaasIntegrationCustomer() : base(0) { }

    private AsaasIntegrationCustomer(long customerId, long companyId, string asaasCustomerId) : base(0)
    {
        CustomerId = customerId;
        CompanyId = companyId;
        AsaasCustomerId = asaasCustomerId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AsaasIntegrationCustomer> Create(long customerId, long companyId, string asaasCustomerId)
    {
        if (customerId <= 0)
            return Result.Failure<AsaasIntegrationCustomer>(
                new Error("CustomerId.Invalid", "CustomerId inválido."));

        if (companyId <= 0)
            return Result.Failure<AsaasIntegrationCustomer>(
                new Error("CompanyId.Invalid", "CompanyId inválido."));

        if (string.IsNullOrWhiteSpace(asaasCustomerId))
            return Result.Failure<AsaasIntegrationCustomer>(
                new Error("AsaasCustomerId.Empty", "O AsaasCustomerId é obrigatório."));

        return Result.Success(new AsaasIntegrationCustomer(customerId, companyId, asaasCustomerId));
    }

    public void UpdateAsaasCustomerId(string newAsaasCustomerId)
    {
        if (!string.IsNullOrWhiteSpace(newAsaasCustomerId))
        {
            AsaasCustomerId = newAsaasCustomerId;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}