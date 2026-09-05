using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AsaasIntegrationSavedCard : AggregateRoot
{
    public long CustomerId { get; private set; }
    public long CompanyId { get; private set; }
    public string CreditCardToken { get; private set; } = string.Empty;
    public string CardBrand { get; private set; } = string.Empty;
    public string Last4Digits { get; private set; } = string.Empty;
    public string? HolderName { get; private set; }
    public string? ExpiryMonth { get; private set; }
    public string? ExpiryYear { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AsaasIntegrationSavedCard() : base(0) { }

    private AsaasIntegrationSavedCard(
        long customerId,
        long companyId,
        string creditCardToken,
        string cardBrand,
        string last4Digits,
        string? holderName,
        string? expiryMonth,
        string? expiryYear,
        bool isDefault) : base(0)
    {
        CustomerId = customerId;
        CompanyId = companyId;
        CreditCardToken = creditCardToken;
        CardBrand = cardBrand;
        Last4Digits = last4Digits;
        HolderName = holderName;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        IsDefault = isDefault;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AsaasIntegrationSavedCard> Create(
        long customerId,
        long companyId,
        string creditCardToken,
        string cardBrand,
        string last4Digits,
        string? holderName = null,
        string? expiryMonth = null,
        string? expiryYear = null,
        bool isDefault = false)
    {
        if (customerId <= 0)
            return Result.Failure<AsaasIntegrationSavedCard>(new Error("CustomerId.Invalid", "CustomerId inválido."));

        if (companyId <= 0)
            return Result.Failure<AsaasIntegrationSavedCard>(new Error("CompanyId.Invalid", "CompanyId inválido."));

        if (string.IsNullOrWhiteSpace(creditCardToken))
            return Result.Failure<AsaasIntegrationSavedCard>(new Error("CreditCardToken.Empty", "Token do cartão obrigatório."));

        return Result.Success(new AsaasIntegrationSavedCard(
            customerId, companyId, creditCardToken, cardBrand, last4Digits, holderName, expiryMonth, expiryYear, isDefault));
    }

    public void UpdateDetails(
        string? holderName = null,
        string? expiryMonth = null,
        string? expiryYear = null,
        bool? isDefault = null)
    {
        if (!string.IsNullOrWhiteSpace(holderName))
            HolderName = holderName;

        if (!string.IsNullOrWhiteSpace(expiryMonth))
            ExpiryMonth = expiryMonth;

        if (!string.IsNullOrWhiteSpace(expiryYear))
            ExpiryYear = expiryYear;

        if (isDefault.HasValue)
            IsDefault = isDefault.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAsDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}