namespace SyncBar.Application.Abstractions.Integrations.Keeta
{
    public sealed record KeetaCredentials(string BaseUrl, string ClientId, string ClientSecret, string AppId);
}
