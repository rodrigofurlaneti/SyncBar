namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    public sealed record CreditCardRequest(string HolderName, string Number, string ExpiryMonth, string ExpiryYear, string Ccv);
}
