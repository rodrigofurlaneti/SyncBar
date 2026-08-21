using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Cliente HTTP real do módulo Logistics do iFood (fase 7, entrega por frota própria) —
/// endpoints e formatos confirmados em 2026-08-20 contra a documentação oficial (Postman
/// collection "Logistics") colada pelo usuário: todas as ações (exceto verifyDeliveryCode)
/// retornam 202 Accepted sem corpo; verifyDeliveryCode retorna 200 com {success: boolean}, e
/// pode devolver 412 Precondition Failed se o pedido ainda não foi recebido ou não é
/// self-delivery (tratado como falha de negócio explicada em ErrorMessage, não erro genérico).
/// </summary>
internal sealed class IFoodLogisticsClient(HttpClient httpClient) : IIFoodLogisticsClient
{
    private const string BaseUrl = "https://merchant-api.ifood.com.br/logistics/v1.0";

    public Task<IFoodLogisticsActionResult> AssignDriverAsync(
        string accessToken, string ifoodOrderId, string workerName, string workerPhone, string workerVehicleType,
        CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/assignDriver", accessToken,
            new { workerName, workerPhone, workerVehicleType }, cancellationToken);

    public Task<IFoodLogisticsActionResult> GoingToOriginAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/goingToOrigin", accessToken, null, cancellationToken);

    public Task<IFoodLogisticsActionResult> ArrivedAtOriginAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/arrivedAtOrigin", accessToken, null, cancellationToken);

    public Task<IFoodLogisticsActionResult> DispatchAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/dispatch", accessToken, null, cancellationToken);

    public Task<IFoodLogisticsActionResult> ArrivedAtDestinationAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/arrivedAtDestination", accessToken, null, cancellationToken);

    public async Task<IFoodVerifyDeliveryCodeResult> VerifyDeliveryCodeAsync(
        string accessToken, string ifoodOrderId, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/orders/{ifoodOrderId}/verifyDeliveryCode")
            {
                Content = JsonContent.Create(new { code }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                return new IFoodVerifyDeliveryCodeResult(false, false,
                    "O pedido ainda não foi recebido pelo iFood ou não é uma entrega por frota própria.");

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodVerifyDeliveryCodeResult(false, false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            var payload = await response.Content.ReadFromJsonAsync<VerifyDeliveryCodeResponseDto>(cancellationToken: cancellationToken);
            return new IFoodVerifyDeliveryCodeResult(true, payload?.Success ?? false, null);
        }
        catch (Exception ex)
        {
            return new IFoodVerifyDeliveryCodeResult(false, false, ex.Message);
        }
    }

    public async Task<IFoodLogisticsOrderDetailsResult> GetOrderDetailsAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/orders/{ifoodOrderId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new IFoodLogisticsOrderDetailsResult(false, null, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");

            // Resposta documentada só como "<object>" — sem schema (ver ressalva na interface).
            // Devolvida crua; quem consumir decide o que extrair.
            return new IFoodLogisticsOrderDetailsResult(true, body, null);
        }
        catch (Exception ex)
        {
            return new IFoodLogisticsOrderDetailsResult(false, null, ex.Message);
        }
    }

    private async Task<IFoodLogisticsActionResult> PostActionAsync(string url, string accessToken, object? payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (payload is not null)
                request.Content = JsonContent.Create(payload);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IFoodLogisticsActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IFoodLogisticsActionResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IFoodLogisticsActionResult(false, ex.Message);
        }
    }

    private static string Truncate(string value) => value.Length > 300 ? value[..300] + "…" : value;

    private sealed record VerifyDeliveryCodeResponseDto(bool Success);
}
