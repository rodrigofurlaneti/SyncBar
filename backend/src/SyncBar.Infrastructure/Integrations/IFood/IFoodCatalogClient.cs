using System.Net.Http.Headers;
using System.Net.Http.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Cliente HTTP real do módulo Catalog do iFood — endpoints e formatos confirmados em 2026-08-19
/// contra a documentação oficial colada pelo usuário. Cobre o "fluxo essencial": criar categoria,
/// criar/atualizar item simples, pausar/reativar item e definir estoque. Ver comentário completo
/// em IIFoodCatalogClient sobre o que fica de fora nesta fase, e sobre o nível de confiança dos
/// nomes de campo de optionGroups/options (Fase 6a).
/// </summary>
internal sealed class IFoodCatalogClient(HttpClient httpClient) : IIFoodCatalogClient
{
    private const string BaseUrl = "https://merchant-api.ifood.com.br/catalog/v2.0";

    public async Task<IFoodCreateCategoryResult> CreateCategoryAsync(string accessToken, string merchantId, string name, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/merchants/{merchantId}/categories")
        {
            Content = JsonContent.Create(new { name, status = "AVAILABLE", template = "DEFAULT" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodCreateCategoryResult(false, null, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            var dto = await response.Content.ReadFromJsonAsync<CategoryResponseDto>(cancellationToken: cancellationToken);
            if (dto?.Id is null)
                return new IFoodCreateCategoryResult(false, null, "iFood não retornou o id da categoria criada.");

            return new IFoodCreateCategoryResult(true, dto.Id, null);
        }
        catch (Exception ex)
        {
            return new IFoodCreateCategoryResult(false, null, ex.Message);
        }
    }

    public Task<IFoodCatalogActionResult> UpsertItemAsync(string accessToken, string merchantId, IFoodUpsertItemRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            item = new
            {
                id = request.ItemId.ToString(),
                type = "DEFAULT",
                categoryId = request.IFoodCategoryId,
                status = request.Available ? "AVAILABLE" : "UNAVAILABLE",
                price = new { value = request.Price },
                externalCode = request.ExternalCode,
            },
            products = new[]
            {
                new
                {
                    id = request.ProductId.ToString(),
                    name = request.ProductName,
                    description = request.ProductDescription,
                    externalCode = request.ProductExternalCode,
                },
            },
            // Fase 6a (extensão): grupos de complemento reais quando o produto tiver
            // ProductComplementGroup vinculado — vazio (comportamento anterior) caso contrário.
            optionGroups = (request.OptionGroups ?? []).Select(og => new
            {
                id = og.GroupId.ToString(),
                name = og.Name,
                status = "AVAILABLE",
                min = og.MinOptions,
                max = og.MaxOptions,
                options = og.Options.Select(o => new
                {
                    id = o.OptionId.ToString(),
                    status = o.Available ? "AVAILABLE" : "UNAVAILABLE",
                    price = new { value = o.Price },
                    product = new
                    {
                        id = o.ProductId.ToString(),
                        name = o.Name,
                    },
                }).ToArray(),
            }).ToArray(),
            // "options" no nível raiz do payload é usado só por combos (Fase 6c) — continua vazio.
            options = Array.Empty<object>(),
        };

        return SendActionAsync(HttpMethod.Put, $"{BaseUrl}/merchants/{merchantId}/items", accessToken, payload, cancellationToken);
    }

    public Task<IFoodCatalogActionResult> SetItemStatusAsync(string accessToken, string merchantId, Guid itemId, bool available, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Patch, $"{BaseUrl}/merchants/{merchantId}/items/status", accessToken,
            new { itemId = itemId.ToString(), status = available ? "AVAILABLE" : "UNAVAILABLE" },
            cancellationToken);

    public Task<IFoodCatalogActionResult> SetInventoryAsync(string accessToken, string merchantId, Guid productId, int quantity, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Post, $"{BaseUrl}/merchants/{merchantId}/inventory", accessToken,
            new { productId = productId.ToString(), quantity },
            cancellationToken);

    private async Task<IFoodCatalogActionResult> SendActionAsync(HttpMethod method, string url, string accessToken, object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(payload) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IFoodCatalogActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IFoodCatalogActionResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IFoodCatalogActionResult(false, ex.Message);
        }
    }

    private static string Truncate(string value) => value.Length > 300 ? value[..300] + "…" : value;

    // DTO interno de desserialização — ReadFromJsonAsync sem options explícitas já é
    // case-insensitive por padrão (mesmo padrão usado em IFoodAuthClient/IFoodOrderClient).
    private sealed record CategoryResponseDto(string? Id);
}
