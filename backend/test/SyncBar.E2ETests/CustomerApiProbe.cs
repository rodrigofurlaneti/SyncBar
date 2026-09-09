using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace SyncBar.E2ETests;

internal sealed class CustomerApiProbe(CustomerTestSettings settings, ITestOutputHelper output) : IDisposable
{
    private readonly HttpClient client = new() { BaseAddress = settings.BaseUrl, Timeout = TimeSpan.FromSeconds(30) };

    public async Task<long> CompanyId()
    {
        var menu = await Request(HttpMethod.Get, $"/api/storefront/branches/{settings.BranchId}/menu");
        var id = menu.GetProperty("companyId").GetInt64();
        Assert.True(id > 0, "O cardápio precisa identificar a empresa da filial.");
        return id;
    }

    public async Task<long> Register(CustomerTestData data)
    {
        var result = await Request(HttpMethod.Post, $"/api/storefront/branches/{settings.BranchId}/customers", new
        { userName = data.Name, email = data.Email, password = data.Password, cpf = data.Cpf });
        var id = result.GetProperty("id").GetInt64();
        Assert.True(id > 0, "Cadastro não retornou um CustomerId válido.");
        return id;
    }

    public async Task VerifyAccountAndAddress(CustomerTestData data, long companyId, long? expectedCustomerId, bool createAddress)
    {
        var login = await Request(HttpMethod.Post, "/api/auth/customer-login", new
        { email = data.Email, password = data.Password, companyId, branchId = settings.BranchId });
        var customerId = login.GetProperty("customerId").GetInt64();
        Assert.True(customerId > 0);
        if (expectedCustomerId.HasValue) Assert.Equal(expectedCustomerId.Value, customerId);
        Assert.Equal(companyId, login.GetProperty("companyId").GetInt64());
        Assert.Equal(data.Name, login.GetProperty("userName").GetString());
        var access = login.GetProperty("accessToken").GetString()!;
        if (createAddress)
            await Request(HttpMethod.Post, "/api/storefront/customer/addresses", new
            { branchId = settings.BranchId, street = "Endereço sintético E2E", number = "1", supplement = "Teste automatizado", zipCode = CustomerTestData.ZipCode }, access);

        var refreshed = await Request(HttpMethod.Post, "/api/auth/customer-refresh", new
        { refreshToken = login.GetProperty("refreshToken").GetString() });
        Assert.Equal(customerId, refreshed.GetProperty("customerId").GetInt64());
        Assert.Equal(companyId, refreshed.GetProperty("companyId").GetInt64());
        var addresses = await Request(HttpMethod.Get, $"/api/storefront/customer/addresses/customer/{customerId}",
            token: refreshed.GetProperty("accessToken").GetString());
        Assert.Contains(addresses.EnumerateArray(), item =>
            item.GetProperty("customerId").GetInt64() == customerId &&
            new string((item.GetProperty("zipCode").GetString() ?? "").Where(char.IsDigit).ToArray()) == CustomerTestData.ZipCode);
        output.WriteLine($"OPERACIONAL: {data.Name}; CustomerId={customerId}; filial={settings.BranchId}; CEP={CustomerTestData.ZipCode}. Cadastro, login, renovação e endereço persistido confirmados.");
    }

    private async Task<JsonElement> Request(HttpMethod method, string path, object? body = null, string? token = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        output.WriteLine($"{DateTime.UtcNow:O} {method} {path}");
        using var response = await client.SendAsync(request);
        // Do not dump response/request bodies: they may include passwords, tokens or personal data.
        Assert.True(response.IsSuccessStatusCode, $"{method} {path} retornou HTTP {(int)response.StatusCode}. Consulte o log da API neste horário.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    public void Dispose() => client.Dispose();
}
