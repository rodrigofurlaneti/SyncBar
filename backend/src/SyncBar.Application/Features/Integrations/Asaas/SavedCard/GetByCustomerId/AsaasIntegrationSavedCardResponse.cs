namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId
{
    public sealed record AsaasIntegrationSavedCardResponse(
        long Id,
        long CustomerId,
        long CompanyId,
        string CardBrand,
        string Last4Digits,
        string HolderName,
        string ExpiryMonth,
        string ExpiryYear,
        bool IsDefault,
        DateTime CreatedAt,
        bool IsActive);
}
