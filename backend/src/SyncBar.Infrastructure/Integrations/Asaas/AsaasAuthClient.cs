using System.Net.Http.Headers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SyncBar.Infrastructure.Integrations.Asaas;

public class AsaasAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly AsaasSettings _settings;
    private readonly bool _isProduction;

    public AsaasAuthClient(
        HttpClient httpClient,
        IOptions<AsaasSettings> options,
        IHostEnvironment env)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _isProduction = env.IsProduction();

        ConfigureClient();
    }

    private void ConfigureClient()
    {
        var baseUrl = _isProduction ? _settings.BaseUrl : _settings.BaseUrlSandBox;
        var apiKey = _isProduction ? _settings.ApiKey : _settings.ApiKeySandBox;

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException($"Asaas BaseUrl não configurada para o ambiente {(_isProduction ? "Produção" : "Sandbox")}.");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Asaas ApiKey não configurada para o ambiente {(_isProduction ? "Produção" : "Sandbox")}.");

        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }

        _httpClient.BaseAddress = new Uri(baseUrl);

        // Configura cabeçalhos apenas se não estiverem presentes
        if (!_httpClient.DefaultRequestHeaders.Contains("access_token"))
        {
            _httpClient.DefaultRequestHeaders.Add("access_token", apiKey);
        }

        if (_httpClient.DefaultRequestHeaders.Accept.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
        }
    }

    public HttpClient Client => _httpClient;

    public string GetWebhookKey() => _isProduction ? _settings.WebhookKey : _settings.WebhookKeySandBox;
}