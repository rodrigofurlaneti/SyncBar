using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SyncBar.Application.Abstractions.Integrations.Keeta;

namespace SyncBar.Infrastructure.Integrations.Keeta;

public sealed class KeetaAuthClient : IKeetaAuthClient
{
    private readonly HttpClient _http;
    private readonly IKeetaCredentialsResolver _credentialsResolver;

    public KeetaAuthClient(HttpClient httpClient, IKeetaCredentialsResolver credentialsResolver)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _credentialsResolver = credentialsResolver;

        if (_http.DefaultRequestHeaders.Accept.Count == 0)
        {
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SyncBar", "1.0"));
        }
    }

    private async Task<KeetaCredentials> ConfigureForTenantAsync(long companyId, long branchId, CancellationToken cancellationToken)
    {
        var credentials = await _credentialsResolver.ResolveAsync(companyId, branchId, cancellationToken);


        return credentials;
    }

    public async Task<string> GetAuthorizationUrlAsync(long companyId, long branchId, string redirectUri, CancellationToken cancellationToken = default)
    {
        var credentials = await ConfigureForTenantAsync(companyId, branchId, cancellationToken);

        var query = $"oauth/authorization/url?clientId={Uri.EscapeDataString(credentials.ClientId)}&redirectUri={Uri.EscapeDataString(redirectUri)}";
        using var request = KeetaSignedRequest.Create(credentials, HttpMethod.Get, query);
        using var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessOrThrowKeetaErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<KeetaAuthorizationUrlDto>(cancellationToken: cancellationToken);
        return result!.MerchantAuthorizationUrl;
    }

    public async Task<KeetaTokenResponse> GetAccessTokenAsync(long companyId, long branchId, CancellationToken cancellationToken = default)
    {
        var credentials = await ConfigureForTenantAsync(companyId, branchId, cancellationToken);

        var payload = new KeetaClientCredentialsRequestDto
        {
            ClientId = credentials.ClientId,
            ClientSecret = credentials.ClientSecret,
            GrantType = "app_level_token"
        };

        using var request = KeetaSignedRequest.Create(credentials, HttpMethod.Post, "oauth/token", payload);
        using var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessOrThrowKeetaErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<KeetaTokenResponseDto>(cancellationToken: cancellationToken);
        return new KeetaTokenResponse(result!.AccessToken, result.TokenType, result.ExpiresIn);
    }

    public async Task<KeetaMerchantInfoResponse> GetMerchantInfoAsync(
        long companyId,
        long branchId,
        string authId,
        string accessToken,
        int pageNum = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var credentials = await ConfigureForTenantAsync(companyId, branchId, cancellationToken);

        using var request = KeetaSignedRequest.Create(credentials, HttpMethod.Get, $"oauth/authorized/{Uri.EscapeDataString(authId)}/merchantInfo?pageNum={pageNum}&pageSize={pageSize}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessOrThrowKeetaErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<KeetaMerchantDataDto>(cancellationToken: cancellationToken);

        var shops = result!.AuthorizedShops
            .Select(s => new KeetaAuthorizedShop(s.Id, s.Name, s.Address, s.Longitude, s.Latitude, s.TimeZone))
            .ToList();

        return new KeetaMerchantInfoResponse(result.UserId, result.BrandId, result.BrandName, shops);
    }

    private static async Task EnsureSuccessOrThrowKeetaErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Keeta API retornou {(int)response.StatusCode} ({response.StatusCode}): {body}");
    }

    private sealed class KeetaAuthorizationUrlDto
    {
        [JsonPropertyName("merchantAuthorizationUrl")]
        public string MerchantAuthorizationUrl { get; set; } = string.Empty;
    }

    private sealed class KeetaClientCredentialsRequestDto
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = string.Empty;

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;
    }

    private sealed class KeetaTokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class KeetaMerchantDataDto
    {
        [JsonPropertyName("userId")]
        public long UserId { get; set; }

        [JsonPropertyName("brandId")]
        public long BrandId { get; set; }

        [JsonPropertyName("brandName")]
        public string BrandName { get; set; } = string.Empty;

        [JsonPropertyName("authorizedShops")]
        public List<KeetaAuthorizedShopDto> AuthorizedShops { get; set; } = [];
    }

    private sealed class KeetaAuthorizedShopDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; } = string.Empty;
    }
}
