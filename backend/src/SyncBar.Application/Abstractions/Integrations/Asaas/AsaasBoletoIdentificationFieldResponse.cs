namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    public sealed record AsaasBoletoIdentificationFieldResponse(string IdentificationField, string? BarCode, string? NossoNumero);
}
