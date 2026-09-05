namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    public sealed record AsaasPixQrCodeResponse(string EncodedImage, string Payload, string? ExpirationDate);
}
