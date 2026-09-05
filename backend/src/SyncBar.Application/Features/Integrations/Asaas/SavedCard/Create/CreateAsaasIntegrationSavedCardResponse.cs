namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create
{
    public sealed record CreateAsaasIntegrationSavedCardResponse(
        long Id,
        long CustomerId,
        long CompanyId,
        string CardBrand,
        string Last4Digits,
        bool IsDefault);
}
