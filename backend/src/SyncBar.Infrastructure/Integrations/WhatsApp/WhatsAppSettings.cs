namespace SyncBar.Infrastructure.Integrations.WhatsApp
{
    public class WhatsAppSettings
    {
        public string BaseUrl { get; set; } = "https://zap.sistemapocket.com.br/rest-api/";
        public string Token { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public string OutboxPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "data", "whatsapp-outbox");
    }
}
