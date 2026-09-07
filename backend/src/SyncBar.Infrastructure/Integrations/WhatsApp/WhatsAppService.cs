using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncBar.Application.Abstractions.Notifications;
using SyncBar.Domain.Primitives;

namespace SyncBar.Infrastructure.Integrations.WhatsApp
{
    public sealed class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _http;
        private readonly WhatsAppSettings _settings;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(HttpClient httpClient, IOptions<WhatsAppSettings> settings, ILogger<WhatsAppService> logger)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<Result> SendImageAsync(string phoneNumber, string message, string fileUrl, CancellationToken cancellationToken = default)
        {
            var payload = new Dictionary<string, string>
            {
                ["token"] = _settings.Token,
                ["id_conexao"] = _settings.ConnectionId,
                ["numero"] = phoneNumber,
                ["mensagem"] = message,
                ["fileurl"] = fileUrl
            };

            try
            {
                using var content = new FormUrlEncodedContent(payload);
                using var response = await _http.PostAsync("sendImage", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Falha ao enviar imagem via WhatsApp. Status: {StatusCode}. Resposta: {Body}",
                        response.StatusCode, body);

                    return Result.Failure(Error.Failure(
                        "WhatsApp.SendFailed",
                        $"A API do WhatsApp retornou status {(int)response.StatusCode}."));
                }

                return Result.Success();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout ao enviar imagem via WhatsApp para {PhoneNumber}.", phoneNumber);
                return Result.Failure(Error.Failure("WhatsApp.Timeout", "Tempo limite excedido ao chamar a API do WhatsApp."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Erro de rede ao enviar imagem via WhatsApp para {PhoneNumber}.", phoneNumber);
                return Result.Failure(Error.Failure("WhatsApp.NetworkError", "Falha de comunicação com a API do WhatsApp."));
            }
        }
    }
}
