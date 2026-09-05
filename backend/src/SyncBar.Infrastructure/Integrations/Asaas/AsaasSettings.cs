namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public class AsaasSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string BaseUrlSandBox { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiKeySandBox { get; set; } = string.Empty;
        public string WebhookKey { get; set; } = string.Empty;
        public string WebhookKeySandBox { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public string WebhookUrlSandBox { get; set; } = string.Empty;
    }
}
