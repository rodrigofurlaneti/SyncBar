using System.Net.Http.Json;
using System.Text.Json;

namespace SyncBar.Infrastructure.Integrations.Asaas;

public class AsaasService : IAsaasService
{
    private readonly HttpClient _http;

    public AsaasService(AsaasAuthClient authClient)
    {
        _http = authClient.Client;
    }

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

        var response = await _http.PostAsJsonAsync("customers", payload, cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);

        var result = await response.Content.ReadFromJsonAsync<AsaasCustomerResponse>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task DeleteCustomerAsync(string asaasCustomerId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"customers/{asaasCustomerId}", cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);
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

        var response = await _http.PostAsJsonAsync("payments", payload, cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);

        var result = await response.Content.ReadFromJsonAsync<AsaasPaymentResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<AsaasPixQrCodeResponse> GetPixQrCodeAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"payments/{paymentId}/pixQrCode", cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);

        var result = await response.Content.ReadFromJsonAsync<AsaasPixQrCodeResponse>(cancellationToken: cancellationToken);
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

        var response = await _http.PostAsJsonAsync("payments", payload, cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);

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

        var response = await _http.PostAsJsonAsync("payments", payload, cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);

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

        var response = await _http.PostAsJsonAsync("creditCard/tokenizeCreditCard", payload, cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);

        var result = await response.Content.ReadFromJsonAsync<AsaasTokenizeCreditCardResponse>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task DeletePaymentAsync(string asaasPaymentId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"payments/{asaasPaymentId}", cancellationToken);
        await EnsureSuccessOrThrowAsaasErrorAsync(response);
    }

    private static async Task EnsureSuccessOrThrowAsaasErrorAsync(HttpResponseMessage response)
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