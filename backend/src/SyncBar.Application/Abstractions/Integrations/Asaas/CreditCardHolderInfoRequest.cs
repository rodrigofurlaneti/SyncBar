namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    public sealed record CreditCardHolderInfoRequest(string Name, string Email, string CpfCnpj, string PostalCode, string AddressNumber, string? Phone = null);
}
