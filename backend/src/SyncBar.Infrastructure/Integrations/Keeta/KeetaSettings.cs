namespace SyncBar.Infrastructure.Integrations.Keeta
{
    public class KeetaSettings
    {
        public string BaseUrl { get; set; } = "https://open.mykeeta.com/api/open/opendelivery";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
    }
}
