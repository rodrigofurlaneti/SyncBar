using System.Net.Http.Headers;
using System.Net.Http.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Implementação real do OAuth2 do iFood (Merchant API) — fluxo <c>client_credentials</c>.
///
/// ENDPOINT E PAYLOAD CONFIRMADOS em 2026-08-19 contra a página oficial "Authentication ›
/// Introdução" (developer.ifood.com.br), colada pelo usuário: endpoint, nomes de campo
/// (grantType/clientId/clientSecret) e content-type (x-www-form-urlencoded) batem exatamente
/// com o que já estava implementado aqui.
///
/// AINDA EM ABERTO: a doc distingue "Fluxo para aplicativos centralizados" de "Fluxo para
/// aplicativos distribuídos" — não confirmamos ainda qual dos dois se aplica ao SyncBar. Como
/// o modelo de dados aqui é 1 client_id/client_secret POR FILIAL (cada loja com seu próprio
/// app/merchant no iFood), a hipótese de trabalho é "distribuído" (client_credentials sozinho
/// já dá acesso ao merchant daquele client_id, sem precisar de authorizationCode/consentimento
/// por merchant). Se o SyncBar algum dia vender pra múltiplos restaurantes sob UM único app
/// iFood, isso muda para o fluxo centralizado (authorizationCode + listagem de merchants
/// autorizados) — não implementado aqui.
///
/// Ciclo de vida do token (doc oficial, não hardcoded no código — usar o `expiresIn` da
/// resposta, nunca um tempo fixo): access token expira em ~3h, refresh token em ~168h (7 dias),
/// authorizationCode em ~5min, código de vínculo em ~10min. Renovar com grantType=refresh_token
/// antes de esgotar, e tratar HTTP 401 como sinal de "token expirado, renove". Isso importa
/// para a fase de polling de pedidos (ainda não implementada) — o teste de conexão desta tela
/// pega um token novo a cada chamada e não guarda/reaproveita nada.
/// </summary>
internal sealed class IFoodAuthClient(HttpClient httpClient) : IIFoodAuthClient
{
    private const string TokenEndpoint = "https://merchant-api.ifood.com.br/authentication/v1.0/oauth/token";

    public async Task<IFoodAuthResult> AuthenticateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grantType"] = "client_credentials",
                    ["clientId"] = clientId,
                    ["clientSecret"] = clientSecret,
                }),
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodAuthResult(false, null, null, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            var payload = await response.Content.ReadFromJsonAsync<IFoodTokenResponse>(cancellationToken: cancellationToken);
            if (payload?.AccessToken is null)
                return new IFoodAuthResult(false, null, null, "Resposta do iFood sem accessToken — formato pode ter mudado.");

            return new IFoodAuthResult(true, payload.AccessToken, payload.ExpiresIn, null);
        }
        catch (Exception ex)
        {
            // Rede indisponível, timeout, DNS, JSON inesperado etc. — nunca deixa subir: quem
            // chama trata como "não conectado" e mostra uma mensagem amigável no lugar de 500.
            return new IFoodAuthResult(false, null, null, ex.Message);
        }
    }

    private static string Truncate(string value) => value.Length > 300 ? value[..300] + "…" : value;

    private sealed record IFoodTokenResponse(string? AccessToken, int? ExpiresIn, string? TokenType);
}
