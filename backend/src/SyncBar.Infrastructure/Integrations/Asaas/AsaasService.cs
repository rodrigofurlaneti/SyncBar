using System.Net.Http.Json;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.Asaas;

namespace SyncBar.Infrastructure.Integrations.Asaas;

public class AsaasService : IAsaasService
{
    private readonly HttpClient _http;
    private readonly IAsaasCredentialsResolver _credentialsResolver;
    private string? _baseUrl;
    private string? _apiKey;

    public AsaasService(AsaasAuthClient authClient, IAsaasCredentialsResolver credentialsResolver)
    {
        _http = authClient.Client;
        _credentialsResolver = credentialsResolver;
    }

    public async Task ConfigureForTenantAsync(long companyId, long? branchId, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsResolver.ResolveAsync(companyId, branchId, cancellationToken);
        _baseUrl = credentials.BaseUrl.TrimEnd('/');
        _apiKey = credentials.ApiKey;
    }

    #region Métodos de Envio HTTP Seguros

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string endpoint,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("AsaasService não foi configurado. Chame ConfigureForTenantAsync antes de realizar operações.");
        }

        var fullUrl = $"{_baseUrl}/{endpoint.TrimStart('/')}";
        using var request = new HttpRequestMessage(method, fullUrl);

        // Header isolado apenas para esta requisição (Thread-safe e sem conflito de HttpClient)
        request.Headers.Add("access_token", _apiKey);

        if (content is not null)
        {
            request.Content = content;
        }

        var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);
        return response;
    }

    #endregion

    public async Task<string> CreateCustomerAsync(
        string name,
        string cpfCnpj,
        string email,
        string? mobilePhone = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            name,
            cpfCnpj,
            email,
            mobilePhone
        };

        using var content = JsonContent.Create(payload);
        using var response = await SendAsync(HttpMethod.Post, "customers", content, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AsaasCustomerResponse>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task DeleteCustomerAsync(string asaasCustomerId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"customers/{asaasCustomerId}", null, cancellationToken);
    }

    public async Task<AsaasPaymentResponse> CreatePixPaymentAsync(
        string customerId,
        decimal value,
        DateTime dueDate,
        string description,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            customer = customerId,
            billingType = "PIX",
            value,
            dueDate = dueDate.ToString("yyyy-MM-dd"),
            description
        };

        using var content = JsonContent.Create(payload);
        using var response = await SendAsync(HttpMethod.Post, "payments", content, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AsaasPaymentResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<AsaasPaymentResponse> CreatePaymentAsync(
        string customerId,
        string billingType,
        decimal value,
        DateTime dueDate,
        string description,
        string? creditCardToken = null,
        int installmentCount = 1,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["customer"] = customerId,
            ["billingType"] = billingType.ToUpperInvariant(),
            ["value"] = value,
            ["dueDate"] = dueDate.ToString("yyyy-MM-dd"),
            ["description"] = description
        };

        if (!string.IsNullOrWhiteSpace(creditCardToken))
        {
            payload["creditCardToken"] = creditCardToken;
        }

        if (installmentCount > 1)
        {
            payload["installmentCount"] = installmentCount;
            payload["totalValue"] = value;
        }

        using var content = JsonContent.Create(payload);
        using var response = await SendAsync(HttpMethod.Post, "payments", content, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AsaasPaymentResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<AsaasPixQrCodeResponse> GetPixQrCodeAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"payments/{paymentId}/pixQrCode", null, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AsaasPixQrCodeResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<AsaasBoletoIdentificationFieldResponse> GetBoletoIdentificationFieldAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"payments/{paymentId}/identificationField", null, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AsaasBoletoIdentificationFieldResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<AsaasCreditCardPaymentResponse> CreateCreditCardPaymentAsync(
        string customerId,
        decimal value,
        DateTime dueDate,
        string description,
        CreditCardRequest card,
        CreditCardHolderInfoRequest holderInfo,
        string? remoteIp = null,
        int installmentCount = 1,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["customer"] = customerId,
            ["billingType"] = "CREDIT_CARD",
            ["value"] = value,
            ["dueDate"] = dueDate.ToString("yyyy-MM-dd"),
            ["description"] = description,
            ["creditCard"] = card,
            ["creditCardHolderInfo"] = holderInfo
        };

        if (installmentCount > 1)
        {
            payload["installmentCount"] = installmentCount;
            payload["totalValue"] = value;
        }

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            payload["remoteIp"] = remoteIp;
        }

        using var content = JsonContent.Create(payload);
        using var response = await SendAsync(HttpMethod.Post, "payments", content, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AsaasCreditCardPaymentResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<AsaasPaymentResponse> CreatePaymentWithCardTokenAsync(
        string customerId,
        decimal value,
        DateTime dueDate,
        string description,
        string creditCardToken,
        int installmentCount = 1,
        string? remoteIp = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["customer"] = customerId,
            ["billingType"] = "CREDIT_CARD",
            ["value"] = value,
            ["dueDate"] = dueDate.ToString("yyyy-MM-dd"),
            ["description"] = description,
            ["creditCardToken"] = creditCardToken
        };

        if (installmentCount > 1)
        {
            payload["installmentCount"] = installmentCount;
            payload["totalValue"] = value;
        }

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            payload["remoteIp"] = remoteIp;
        }

        using var content = JsonContent.Create(payload);
        using var response = await SendAsync(HttpMethod.Post, "payments", content, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AsaasPaymentResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<AsaasTokenizeCreditCardResponse> TokenizeCreditCardAsync(
        string customerId,
        CreditCardRequest card,
        CreditCardHolderInfoRequest? holderInfo = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["customer"] = customerId,
            ["creditCard"] = card
        };

        if (holderInfo is not null)
        {
            payload["creditCardHolderInfo"] = holderInfo;
        }

        using var content = JsonContent.Create(payload);
        using var response = await SendAsync(HttpMethod.Post, "creditCard/tokenizeCreditCard", content, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AsaasTokenizeCreditCardResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task DeletePaymentAsync(string asaasPaymentId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"payments/{asaasPaymentId}", null, cancellationToken);
    }

    internal static async Task EnsureSuccessOrThrowAsaasErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorContent = await response.Content.ReadAsStringAsync();

        try
        {
            var errorResponse = JsonSerializer.Deserialize<AsaasErrorWrapper>(errorContent);
            if (errorResponse?.Errors != null && errorResponse.Errors.Count > 0)
            {
                var messages = string.Join("; ", errorResponse.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new HttpRequestException($"Erro Asaas (HTTP {response.StatusCode}): {messages}");
            }
        }
        catch (JsonException)
        {
            // Ignora erro de parse e lança conteúdo bruto abaixo
        }

        throw new HttpRequestException($"Falha na requisição Asaas (HTTP {response.StatusCode}): {errorContent}");
    }
}