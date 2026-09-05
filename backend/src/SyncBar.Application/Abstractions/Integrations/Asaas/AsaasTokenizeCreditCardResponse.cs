namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    public sealed record AsaasTokenizeCreditCardResponse(string CreditCardToken, string CreditCardBrand, string CreditCardNumber);
}
