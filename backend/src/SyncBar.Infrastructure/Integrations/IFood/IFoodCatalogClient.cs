using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Cliente HTTP real do módulo Catalog do Ifood. Ver comentário completo em IIfoodCatalogClient
/// sobre o "fluxo essencial" (Fase 3/6a) e a cobertura completa (Fase 10 — v2 tipado + v1 via
/// despachante genérico).
/// </summary>
internal sealed class IfoodCatalogClient(HttpClient httpClient) : IIfoodCatalogClient
{
    private const string BaseUrlV2 = "https://merchant-api.Ifood.com.br/catalog/v2.0";
    private const string BaseUrlV1 = "https://merchant-api.Ifood.com.br/catalog/v1.0";

    #region Fluxo essencial (Fase 3/6a)

    public async Task<IfoodCreateCategoryResult> CreateCategoryAsync(string accessToken, string merchantId, string catalogId, string name, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrlV2}/merchants/{merchantId}/catalogs/{catalogId}/categories")
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
                return new IfoodCreateCategoryResult(false, null, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            var dto = await response.Content.ReadFromJsonAsync<CategoryResponseDto>(cancellationToken: cancellationToken);
            if (dto?.Id is null)
                return new IfoodCreateCategoryResult(false, null, "Ifood não retornou o id da categoria criada.");

            return new IfoodCreateCategoryResult(true, dto.Id, null);
        }
        catch (Exception ex)
        {
            return new IfoodCreateCategoryResult(false, null, ex.Message);
        }
    }

    public Task<IfoodCatalogActionResult> UpsertItemAsync(string accessToken, string merchantId, IfoodUpsertItemRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            item = new
            {
                id = request.ItemId.ToString(),
                type = "DEFAULT",
                categoryId = request.IfoodCategoryId,
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

        return SendActionAsync(HttpMethod.Put, $"{BaseUrlV2}/merchants/{merchantId}/items", accessToken, payload, cancellationToken);
    }

    public Task<IfoodCatalogActionResult> SetItemStatusAsync(string accessToken, string merchantId, Guid itemId, bool available, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Patch, $"{BaseUrlV2}/merchants/{merchantId}/items/status", accessToken,
            new { itemId = itemId.ToString(), status = available ? "AVAILABLE" : "UNAVAILABLE" },
            cancellationToken);

    public Task<IfoodCatalogActionResult> SetInventoryAsync(string accessToken, string merchantId, Guid productId, int quantity, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Post, $"{BaseUrlV2}/merchants/{merchantId}/inventory", accessToken,
            new { productId = productId.ToString(), amount = quantity },
            cancellationToken);

    #endregion

    #region Catalogs / Categories / Sellable items (v2)

    public async Task<IfoodCatalogsListResult> GetCatalogsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = AuthedGet($"{BaseUrlV2}/merchants/{merchantId}/catalogs", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodCatalogsListResult(false, [], await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            var list = new List<IfoodCatalogSummaryDto>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var context = el.TryGetProperty("context", out var ctxEl) && ctxEl.ValueKind == JsonValueKind.Array
                        ? ctxEl.EnumerateArray().Select(c => c.GetString() ?? string.Empty).ToArray()
                        : [];
                    list.Add(new IfoodCatalogSummaryDto(
                        GetString(el, "catalogId"), GetString(el, "status"), context, GetString(el, "groupId"), GetDate(el, "modifiedAt")));
                }
            }
            return new IfoodCatalogsListResult(true, list, null);
        }
        catch (Exception ex)
        {
            return new IfoodCatalogsListResult(false, [], ex.Message);
        }
    }

    public async Task<IfoodCategoryListResult> ListCategoriesAsync(string accessToken, string merchantId, string catalogId, bool includeItems = false, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV2}/merchants/{merchantId}/catalogs/{catalogId}/categories?includeItems={(includeItems ? "true" : "false")}";
        try
        {
            using var request = AuthedGet(url, accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodCategoryListResult(false, [], await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            var list = new List<IfoodCategoryDto>();
            // A doc oficial mostra o exemplo de resposta como um objeto de categoria único, mas o
            // path é uma listagem — aceita tanto array quanto objeto único defensivamente.
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                foreach (var el in doc.RootElement.EnumerateArray())
                    list.Add(ParseCategory(el));
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                list.Add(ParseCategory(doc.RootElement));

            return new IfoodCategoryListResult(true, list, null);
        }
        catch (Exception ex)
        {
            return new IfoodCategoryListResult(false, [], ex.Message);
        }
    }

    public async Task<IfoodCategoryDetailResult> GetCategoryAsync(string accessToken, string merchantId, string catalogId, string categoryId, bool includeItems = false, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV2}/merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}?includeItems={(includeItems ? "true" : "false")}";
        return await GetCategoryDetailAsync(url, accessToken, cancellationToken);
    }

    public async Task<IfoodCategoryDetailResult> EditCategoryAsync(string accessToken, string merchantId, string catalogId, string categoryId, string? name, string? externalCode, string? status, int? index, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV2}/merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}";
        var payload = new Dictionary<string, object?>();
        if (name is not null) payload["name"] = name;
        if (externalCode is not null) payload["externalCode"] = externalCode;
        if (status is not null) payload["status"] = status;
        if (index is not null) payload["index"] = index;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(payload) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodCategoryDetailResult(false, null, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            return new IfoodCategoryDetailResult(true, ParseCategory(doc.RootElement), text, null);
        }
        catch (Exception ex)
        {
            return new IfoodCategoryDetailResult(false, null, null, ex.Message);
        }
    }

    public Task<IfoodCatalogActionResult> DeleteCategoryAsync(string accessToken, string merchantId, string categoryId, CancellationToken cancellationToken = default)
        // v2: SEM catalogId no path (diferente de create/get/edit) — confirmado via jq contra a
        // collection oficial (Fase 10).
        => SendDeleteAsync($"{BaseUrlV2}/merchants/{merchantId}/categories/{categoryId}", accessToken, cancellationToken);

    public async Task<IfoodSellableItemsResult> ListSellableItemsAsync(string accessToken, string merchantId, string groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = AuthedGet($"{BaseUrlV2}/merchants/{merchantId}/catalogs/{groupId}/sellableItems", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodSellableItemsResult(false, [], await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            var list = new List<IfoodSellableItemDto>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    decimal? price = null;
                    if (el.TryGetProperty("itemPrice", out var priceEl) && priceEl.TryGetProperty("value", out var vEl) && vEl.ValueKind == JsonValueKind.Number)
                        price = vEl.GetDecimal();

                    list.Add(new IfoodSellableItemDto(
                        GetString(el, "itemId"), GetString(el, "categoryId"), GetString(el, "itemName"),
                        GetString(el, "itemExternalCode"), GetString(el, "itemEan"), price));
                }
            }
            return new IfoodSellableItemsResult(true, list, null);
        }
        catch (Exception ex)
        {
            return new IfoodSellableItemsResult(false, [], ex.Message);
        }
    }

    #endregion

    #region Items (v2 — flat)

    public async Task<IfoodItemFlatResult> GetItemFlatAsync(string accessToken, string merchantId, Guid itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = AuthedGet($"{BaseUrlV2}/merchants/{merchantId}/items/{itemId}/flat", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodItemFlatResult(false, null, null, null, null, null, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            var item = doc.RootElement.TryGetProperty("item", out var itemEl) ? itemEl : doc.RootElement;
            decimal? price = null;
            if (item.TryGetProperty("price", out var priceEl) && priceEl.TryGetProperty("value", out var vEl) && vEl.ValueKind == JsonValueKind.Number)
                price = vEl.GetDecimal();

            return new IfoodItemFlatResult(true, GetString(item, "id"), GetString(item, "status"), price, GetString(item, "externalCode"), GetString(item, "categoryId"), text, null);
        }
        catch (Exception ex)
        {
            return new IfoodItemFlatResult(false, null, null, null, null, null, null, ex.Message);
        }
    }

    public Task<IfoodCatalogActionResult> SetItemPriceAsync(string accessToken, string merchantId, Guid itemId, decimal value, decimal? originalValue, IReadOnlyCollection<IfoodItemPriceByCatalog>? priceByCatalog = null, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Patch, $"{BaseUrlV2}/merchants/{merchantId}/items/price", accessToken,
            new
            {
                itemId = itemId.ToString(),
                price = new { value, originalValue },
                priceByCatalog = (priceByCatalog ?? []).Select(p => new { value = p.Value, catalogContext = p.CatalogContext, originalValue = p.OriginalValue }).ToArray(),
            },
            cancellationToken);

    public Task<IfoodCatalogActionResult> SetItemExternalCodeAsync(string accessToken, string merchantId, Guid itemId, string? externalCode, IReadOnlyCollection<IfoodItemExternalCodeByCatalog>? byCatalog = null, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Patch, $"{BaseUrlV2}/merchants/{merchantId}/items/externalCode", accessToken,
            new
            {
                itemId = itemId.ToString(),
                externalCode,
                externalCodeByCatalog = (byCatalog ?? []).Select(c => new { externalCode = c.ExternalCode, catalogContext = c.CatalogContext }).ToArray(),
            },
            cancellationToken);

    public Task<IfoodCatalogActionResult> DeleteItemAsync(string accessToken, string merchantId, string categoryId, Guid productId, string? catalogContext = null, CancellationToken cancellationToken = default)
        => SendDeleteAsync(
            $"{BaseUrlV2}/merchants/{merchantId}/categories/{categoryId}/products/{productId}" + (catalogContext is null ? "" : $"?catalogContext={Uri.EscapeDataString(catalogContext)}"),
            accessToken, cancellationToken);

    public async Task<IfoodCategoryItemsResult> ListCategoryItemsAsync(string accessToken, string merchantId, string categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = AuthedGet($"{BaseUrlV2}/merchants/{merchantId}/categories/{categoryId}/items", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodCategoryItemsResult(false, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IfoodCategoryItemsResult(true, text, null);
        }
        catch (Exception ex)
        {
            return new IfoodCategoryItemsResult(false, null, ex.Message);
        }
    }

    #endregion

    #region Products (v2)

    public async Task<IfoodProductListResult> ListProductsAsync(string accessToken, string merchantId, int? limit = null, int? page = null, CancellationToken cancellationToken = default)
    {
        var qs = new List<string>();
        if (limit is not null) qs.Add($"limit={limit}");
        if (page is not null) qs.Add($"page={page}");
        var url = $"{BaseUrlV2}/merchants/{merchantId}/products" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return await ListProductsFromUrlAsync(url, accessToken, cancellationToken);
    }

    public async Task<IfoodProductDetailResult> CreateProductAsync(string accessToken, string merchantId, IfoodUpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        var payload = BuildProductPayload(request, includeId: true);
        return await SendProductWriteAsync(HttpMethod.Post, $"{BaseUrlV2}/merchants/{merchantId}/products", accessToken, payload, cancellationToken);
    }

    public async Task<IfoodProductDetailResult> EditProductAsync(string accessToken, string merchantId, Guid productId, IfoodUpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        var payload = BuildProductPayload(request, includeId: false);
        return await SendProductWriteAsync(HttpMethod.Put, $"{BaseUrlV2}/merchants/{merchantId}/products/{productId}", accessToken, payload, cancellationToken);
    }

    public Task<IfoodCatalogActionResult> DeleteProductAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default)
        => SendDeleteAsync($"{BaseUrlV2}/merchants/{merchantId}/products/{productId}", accessToken, cancellationToken);

    public Task<IfoodCatalogActionResult> BatchUpdateProductStatusesAsync(string accessToken, string merchantId, IReadOnlyCollection<IfoodBatchProductStatusItem> items, string? catalogContext = null, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV2}/merchants/{merchantId}/products/status" + (catalogContext is null ? "" : $"?catalogContext={Uri.EscapeDataString(catalogContext)}");
        var payload = items.Select(i => new { status = i.Status, productId = i.ProductId, externalCode = i.ExternalCode, resources = i.Resources ?? [] }).ToArray();
        return SendActionAsync(HttpMethod.Patch, url, accessToken, payload, cancellationToken);
    }

    public async Task<IfoodBatchDispatchResult> BatchUpdateProductPricesAsync(string accessToken, string merchantId, IReadOnlyCollection<IfoodBatchProductPriceItem> items, string? catalogContext = null, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV2}/merchants/{merchantId}/products/price" + (catalogContext is null ? "" : $"?catalogContext={Uri.EscapeDataString(catalogContext)}");
        var payload = items.Select(i => new
        {
            price = new { value = i.Value, originalValue = i.OriginalValue },
            productId = i.ProductId,
            externalCode = i.ExternalCode,
            resources = i.Resources ?? [],
        }).ToArray();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(payload) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodBatchDispatchResult(false, null, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
                return new IfoodBatchDispatchResult(true, null, null, null);

            using var doc = JsonDocument.Parse(text);
            return new IfoodBatchDispatchResult(true, GetString(doc.RootElement, "url"), GetString(doc.RootElement, "batchId"), null);
        }
        catch (Exception ex)
        {
            return new IfoodBatchDispatchResult(false, null, null, ex.Message);
        }
    }

    public async Task<IfoodProductListResult> ListProductsByExternalCodeAsync(string accessToken, string merchantId, string externalCode, CancellationToken cancellationToken = default)
        => await ListProductsFromUrlAsync($"{BaseUrlV2}/merchants/{merchantId}/products/externalCode/{Uri.EscapeDataString(externalCode)}", accessToken, cancellationToken);

    public async Task<IfoodProductDetailResult> GetProductByIdAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = AuthedGet($"{BaseUrlV2}/merchants/{merchantId}/product/{productId}", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodProductDetailResult(false, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            return new IfoodProductDetailResult(true, ParseProduct(doc.RootElement), null);
        }
        catch (Exception ex)
        {
            return new IfoodProductDetailResult(false, null, ex.Message);
        }
    }

    #endregion

    #region Option groups / Options (v2 — manutenção)

    public async Task<IfoodOptionGroupListResult> ListOptionGroupsAsync(string accessToken, string merchantId, bool includeOptions = false, string? catalogContext = null, CancellationToken cancellationToken = default)
    {
        var qs = new List<string> { $"includeOptions={(includeOptions ? "true" : "false")}" };
        if (catalogContext is not null) qs.Add($"catalogContext={Uri.EscapeDataString(catalogContext)}");
        var url = $"{BaseUrlV2}/merchants/{merchantId}/optionGroups?{string.Join("&", qs)}";

        try
        {
            using var request = AuthedGet(url, accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodOptionGroupListResult(false, [], await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            var list = new List<IfoodOptionGroupDto>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                foreach (var el in doc.RootElement.EnumerateArray())
                    list.Add(ParseOptionGroup(el));
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                list.Add(ParseOptionGroup(doc.RootElement));

            return new IfoodOptionGroupListResult(true, list, null);
        }
        catch (Exception ex)
        {
            return new IfoodOptionGroupListResult(false, [], ex.Message);
        }
    }

    public Task<IfoodCatalogActionResult> UpdateOptionGroupAsync(string accessToken, string merchantId, Guid optionGroupId, string name, CancellationToken cancellationToken = default)
        => SendActionAsync(HttpMethod.Patch, $"{BaseUrlV2}/merchants/{merchantId}/optionGroups/{optionGroupId}", accessToken, new { name }, cancellationToken);

    public Task<IfoodCatalogActionResult> DeleteOptionGroupAsync(string accessToken, string merchantId, Guid optionGroupId, CancellationToken cancellationToken = default)
        => SendDeleteAsync($"{BaseUrlV2}/merchants/{merchantId}/optionGroups/{optionGroupId}", accessToken, cancellationToken);

    public Task<IfoodCatalogActionResult> DisassociateOptionGroupFromProductAsync(string accessToken, string merchantId, Guid optionGroupId, Guid productId, CancellationToken cancellationToken = default)
        => SendDeleteAsync($"{BaseUrlV2}/merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}", accessToken, cancellationToken);

    public Task<IfoodCatalogActionResult> DeleteOptionAsync(string accessToken, string merchantId, Guid optionGroupId, Guid productId, string? catalogContext = null, CancellationToken cancellationToken = default)
        => SendDeleteAsync(
            $"{BaseUrlV2}/merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}/option" + (catalogContext is null ? "" : $"?catalogContext={Uri.EscapeDataString(catalogContext)}"),
            accessToken, cancellationToken);

    public Task<IfoodCatalogActionResult> UpdateOptionGroupStatusAsync(string accessToken, string merchantId, Guid optionGroupId, bool available, CancellationToken cancellationToken = default)
        => SendActionAsync(HttpMethod.Patch, $"{BaseUrlV2}/merchants/{merchantId}/optionGroups/{optionGroupId}/status", accessToken, new { status = available ? "AVAILABLE" : "UNAVAILABLE" }, cancellationToken);

    public Task<IfoodCatalogActionResult> SetOptionPriceAsync(string accessToken, string merchantId, Guid optionId, decimal value, decimal? originalValue, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Patch, $"{BaseUrlV2}/merchants/{merchantId}/options/price", accessToken,
            new { optionId = optionId.ToString(), price = new { value, originalValue }, parentCustomizationOptionId },
            cancellationToken);

    public Task<IfoodCatalogActionResult> SetOptionExternalCodeAsync(string accessToken, string merchantId, Guid optionId, string externalCode, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Patch, $"{BaseUrlV2}/merchants/{merchantId}/options/externalCode", accessToken,
            new { optionId = optionId.ToString(), externalCode, parentCustomizationOptionId },
            cancellationToken);

    public Task<IfoodCatalogActionResult> SetOptionStatusAsync(string accessToken, string merchantId, Guid optionId, bool available, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Patch, $"{BaseUrlV2}/merchants/{merchantId}/options/status", accessToken,
            new { optionId = optionId.ToString(), status = available ? "AVAILABLE" : "UNAVAILABLE", parentCustomizationOptionId },
            cancellationToken);

    #endregion

    #region Inventory / Batch results (v2)

    public async Task<IfoodInventoryResult> GetInventoryAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = AuthedGet($"{BaseUrlV2}/merchants/{merchantId}/inventory/{productId}", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodInventoryResult(false, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            var amount = doc.RootElement.TryGetProperty("amount", out var aEl) && aEl.ValueKind == JsonValueKind.Number ? (int?)aEl.GetInt32() : null;
            return new IfoodInventoryResult(true, new IfoodInventoryDto(GetString(doc.RootElement, "productId"), GetString(doc.RootElement, "ownerId"), amount, null), null);
        }
        catch (Exception ex)
        {
            return new IfoodInventoryResult(false, null, ex.Message);
        }
    }

    public Task<IfoodCatalogActionResult> DeleteInventoryBatchAsync(string accessToken, string merchantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Post, $"{BaseUrlV2}/merchants/{merchantId}/inventory/batchDelete", accessToken,
            new { productIds = productIds.Select(p => p.ToString()).ToArray() },
            cancellationToken);

    public async Task<IfoodBatchStatusResult> GetBatchResultAsync(string accessToken, string merchantId, string batchId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = AuthedGet($"{BaseUrlV2}/merchants/{merchantId}/batch/{batchId}", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodBatchStatusResult(false, null, [], await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            var results = new List<IfoodBatchStatusResultItem>();
            if (doc.RootElement.TryGetProperty("results", out var resEl) && resEl.ValueKind == JsonValueKind.Array)
                foreach (var el in resEl.EnumerateArray())
                    results.Add(new IfoodBatchStatusResultItem(GetString(el, "resourceId"), GetString(el, "result"), GetString(el, "failureReason")));

            return new IfoodBatchStatusResult(true, GetString(doc.RootElement, "batchStatus"), results, null);
        }
        catch (Exception ex)
        {
            return new IfoodBatchStatusResult(false, null, [], ex.Message);
        }
    }

    #endregion

    #region Version (v2)

    public async Task<IfoodCatalogVersionResult> CheckVersionAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = AuthedGet($"{BaseUrlV2}/merchants/{merchantId}/catalog/version", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodCatalogVersionResult(false, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            // Resposta é uma string JSON simples (ex.: "\"v2\"") — remove aspas se vierem.
            return new IfoodCatalogVersionResult(true, text.Trim('"'), null);
        }
        catch (Exception ex)
        {
            return new IfoodCatalogVersionResult(false, null, ex.Message);
        }
    }

    public Task<IfoodCatalogActionResult> UpgradeVersionAsync(string accessToken, string merchantId, bool? cleanMigration = null, CancellationToken cancellationToken = default)
        => SendActionAsync(
            HttpMethod.Post,
            $"{BaseUrlV2}/merchants/{merchantId}/version/upgrade" + (cleanMigration is null ? "" : $"?cleanMigration={(cleanMigration.Value ? "true" : "false")}"),
            accessToken, new { }, cancellationToken);

    public Task<IfoodCatalogActionResult> DowngradeVersionAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
        => SendActionAsync(HttpMethod.Post, $"{BaseUrlV2}/merchants/{merchantId}/version/downgrade", accessToken, new { }, cancellationToken);

    #endregion

    #region Image (v2)

    public async Task<IfoodImageUploadResult> UploadImageAsync(string accessToken, string merchantId, string jsonBody, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrlV2}/merchants/{merchantId}/image/upload")
            {
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodImageUploadResult(false, text, $"Ifood retornou {(int)response.StatusCode}: {Truncate(text)}");

            return new IfoodImageUploadResult(true, text, null);
        }
        catch (Exception ex)
        {
            return new IfoodImageUploadResult(false, null, ex.Message);
        }
    }

    #endregion

    #region Catálogo v1 (legado) — despachante genérico

    // Método fixo + template de rota (com placeholders {merchantId}, {catalogId}, {categoryId} etc.)
    // por operação — preenchidos em runtime a partir de routeParams. Extraído via jq direto da
    // collection Postman oficial v1 em 2026-08-21 (ver claude/Ifood-integration-status.md, Fase 10).
    private static readonly IReadOnlyDictionary<IfoodCatalogV1Operation, (HttpMethod Method, string PathTemplate)> V1Operations =
        new Dictionary<IfoodCatalogV1Operation, (HttpMethod, string)>
        {
            [IfoodCatalogV1Operation.ListCatalogs] = (HttpMethod.Get, "merchants/{merchantId}/catalogs"),
            [IfoodCatalogV1Operation.ListUnsellableItems] = (HttpMethod.Get, "merchants/{merchantId}/catalogs/{catalogId}/unsellableItems"),
            [IfoodCatalogV1Operation.ListCategories] = (HttpMethod.Get, "merchants/{merchantId}/catalogs/{catalogId}/categories"),
            [IfoodCatalogV1Operation.CreateCategory] = (HttpMethod.Post, "merchants/{merchantId}/catalogs/{catalogId}/categories"),
            [IfoodCatalogV1Operation.GetCategory] = (HttpMethod.Get, "merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}"),
            [IfoodCatalogV1Operation.EditCategory] = (HttpMethod.Patch, "merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}"),
            [IfoodCatalogV1Operation.DeleteCategory] = (HttpMethod.Delete, "merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}"),
            [IfoodCatalogV1Operation.ListSellableItems] = (HttpMethod.Get, "merchants/{merchantId}/catalogs/{groupId}/sellableItems"),
            [IfoodCatalogV1Operation.EditAisleGroupId] = (HttpMethod.Put, "merchants/{merchantId}/catalog/{catalogId}"),
            [IfoodCatalogV1Operation.UpdateItemStatusByItemId] = (HttpMethod.Patch, "merchants/{merchantId}/catalog/item/{itemId}/status"),
            [IfoodCatalogV1Operation.UpdateOptionStatusByItemIdAndOptionId] = (HttpMethod.Patch, "merchants/{merchantId}/catalog/item/{itemId}/option/{optionItemId}/status"),
            [IfoodCatalogV1Operation.GetItem] = (HttpMethod.Get, "merchants/{merchantId}/items/{itemId}"),
            [IfoodCatalogV1Operation.EditItemStatus] = (HttpMethod.Patch, "merchants/{merchantId}/items/{itemId}/status"),
            [IfoodCatalogV1Operation.CreateItem] = (HttpMethod.Post, "merchants/{merchantId}/categories/{categoryId}/products/{productId}"),
            [IfoodCatalogV1Operation.EditItem] = (HttpMethod.Patch, "merchants/{merchantId}/categories/{categoryId}/products/{productId}"),
            [IfoodCatalogV1Operation.DeleteItem] = (HttpMethod.Delete, "merchants/{merchantId}/categories/{categoryId}/products/{productId}"),
            [IfoodCatalogV1Operation.CreateOptionGroup] = (HttpMethod.Post, "merchants/{merchantId}/optionGroups"),
            [IfoodCatalogV1Operation.ListOptionGroups] = (HttpMethod.Get, "merchants/{merchantId}/optionGroups"),
            [IfoodCatalogV1Operation.UpdateOptionGroup] = (HttpMethod.Patch, "merchants/{merchantId}/optionGroups/{optionGroupId}"),
            [IfoodCatalogV1Operation.DeleteOptionGroup] = (HttpMethod.Delete, "merchants/{merchantId}/optionGroups/{optionGroupId}"),
            [IfoodCatalogV1Operation.AssociateOptionGroupToProduct] = (HttpMethod.Post, "merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}"),
            [IfoodCatalogV1Operation.UpdateOptionGroupProductAssociation] = (HttpMethod.Put, "merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}"),
            [IfoodCatalogV1Operation.DisassociateOptionGroupFromProduct] = (HttpMethod.Delete, "merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}"),
            [IfoodCatalogV1Operation.CreateOption] = (HttpMethod.Post, "merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}/option"),
            [IfoodCatalogV1Operation.UpdateOption] = (HttpMethod.Patch, "merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}/option"),
            [IfoodCatalogV1Operation.DeleteOption] = (HttpMethod.Delete, "merchants/{merchantId}/optionGroups/{optionGroupId}/products/{productId}/option"),
            [IfoodCatalogV1Operation.UpdateOptionGroupStatus] = (HttpMethod.Patch, "merchants/{merchantId}/optionGroups/{optionGroupId}/status"),
            [IfoodCatalogV1Operation.ListProducts] = (HttpMethod.Get, "merchants/{merchantId}/products"),
            [IfoodCatalogV1Operation.CreateProduct] = (HttpMethod.Post, "merchants/{merchantId}/products"),
            [IfoodCatalogV1Operation.EditProduct] = (HttpMethod.Put, "merchants/{merchantId}/products/{productId}"),
            [IfoodCatalogV1Operation.DeleteProduct] = (HttpMethod.Delete, "merchants/{merchantId}/products/{productId}"),
            [IfoodCatalogV1Operation.UpdateProductStatus] = (HttpMethod.Patch, "merchants/{merchantId}/products/{productId}/status"),
            [IfoodCatalogV1Operation.BatchUpdateProductStatuses] = (HttpMethod.Patch, "merchants/{merchantId}/products/status"),
            [IfoodCatalogV1Operation.BatchUpdateProductPrices] = (HttpMethod.Patch, "merchants/{merchantId}/products/price"),
            [IfoodCatalogV1Operation.ListProductsByExternalCode] = (HttpMethod.Get, "merchants/{merchantId}/products/externalCode/{externalCode}"),
            [IfoodCatalogV1Operation.BatchUpdateStatusByExternalCode] = (HttpMethod.Patch, "merchants/{merchantId}/products/externalCode/{externalCode}/status"),
            [IfoodCatalogV1Operation.GetProductById] = (HttpMethod.Get, "merchants/{merchantId}/product/{productId}"),
            [IfoodCatalogV1Operation.CreatePizza] = (HttpMethod.Post, "merchants/{merchantId}/pizzas"),
            [IfoodCatalogV1Operation.ListPizzas] = (HttpMethod.Get, "merchants/{merchantId}/pizzas"),
            [IfoodCatalogV1Operation.UpdatePizza] = (HttpMethod.Put, "merchants/{merchantId}/pizzas/{pizzaId}"),
            [IfoodCatalogV1Operation.UpdatePizzaStatus] = (HttpMethod.Patch, "merchants/{merchantId}/pizzas/{pizzaId}"),
            [IfoodCatalogV1Operation.LinkPizzaToCategory] = (HttpMethod.Post, "merchants/{merchantId}/pizzas/{pizzaId}/categories/{categoryId}"),
            [IfoodCatalogV1Operation.UnlinkPizzaFromCategory] = (HttpMethod.Delete, "merchants/{merchantId}/pizzas/{pizzaId}/categories/{categoryId}"),
            [IfoodCatalogV1Operation.BatchUpdatePizzaPricesByExternalCode] = (HttpMethod.Patch, "merchants/{merchantId}/pizzas/pricesByExternalCode"),
            [IfoodCatalogV1Operation.BatchUpdatePizzaPrices] = (HttpMethod.Post, "merchants/{merchantId}/pizzas/prices"),
            [IfoodCatalogV1Operation.GetBatchResults] = (HttpMethod.Get, "merchants/{merchantId}/batch/{batchId}"),
            [IfoodCatalogV1Operation.UpsertInventory] = (HttpMethod.Post, "merchants/{merchantId}/inventory"),
            [IfoodCatalogV1Operation.GetInventory] = (HttpMethod.Get, "merchants/{merchantId}/inventory/{productId}"),
            [IfoodCatalogV1Operation.DeleteInventoryBatch] = (HttpMethod.Post, "merchants/{merchantId}/inventory/batchDelete"),
            [IfoodCatalogV1Operation.MultisetupUpsertItem] = (HttpMethod.Put, "merchants/{merchantId}/multisetup/items"),
            [IfoodCatalogV1Operation.MultisetupUpdateOptionPrice] = (HttpMethod.Patch, "merchants/{merchantId}/multisetup/options/price"),
            [IfoodCatalogV1Operation.MultisetupUpdateOptionStatus] = (HttpMethod.Patch, "merchants/{merchantId}/multisetup/options/status"),
            [IfoodCatalogV1Operation.MultisetupDeleteCategory] = (HttpMethod.Delete, "merchants/{merchantId}/multisetup/categories/{categoryId}"),
            [IfoodCatalogV1Operation.MultisetupListCategoryItems] = (HttpMethod.Get, "merchants/{merchantId}/multisetup/categories/{categoryId}/items"),
            [IfoodCatalogV1Operation.MultisetupDeleteOptionGroup] = (HttpMethod.Delete, "merchants/{merchantId}/multisetup/optionGroups/{optionGroupId}"),
            [IfoodCatalogV1Operation.MultisetupIsMultisetup] = (HttpMethod.Get, "merchants/{merchantId}/multisetup/isMultisetup"),
        };

    public async Task<IfoodRawApiResult> InvokeCatalogV1Async(
        string accessToken, string merchantId, IfoodCatalogV1Operation operation,
        IReadOnlyDictionary<string, string>? routeParams = null,
        IReadOnlyDictionary<string, string>? queryParams = null,
        string? jsonBody = null,
        CancellationToken cancellationToken = default)
    {
        if (!V1Operations.TryGetValue(operation, out var op))
            return new IfoodRawApiResult(false, 0, null, $"Operação {operation} não mapeada.");

        var path = op.PathTemplate.Replace("{merchantId}", Uri.EscapeDataString(merchantId));
        foreach (var kv in routeParams ?? new Dictionary<string, string>())
            path = path.Replace("{" + kv.Key + "}", Uri.EscapeDataString(kv.Value));

        if (path.Contains('{'))
            return new IfoodRawApiResult(false, 0, null, $"Faltam parâmetros de rota para {operation}: {path}");

        var url = $"{BaseUrlV1}/{path}";
        if (queryParams is { Count: > 0 })
            url += "?" + string.Join("&", queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        try
        {
            using var request = new HttpRequestMessage(op.Method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (jsonBody is not null && op.Method != HttpMethod.Get && op.Method != HttpMethod.Delete)
                request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IfoodRawApiResult(response.IsSuccessStatusCode, (int)response.StatusCode, text, response.IsSuccessStatusCode ? null : $"Ifood retornou {(int)response.StatusCode}: {Truncate(text)}");
        }
        catch (Exception ex)
        {
            return new IfoodRawApiResult(false, 0, null, ex.Message);
        }
    }

    #endregion

    #region Helpers

    private static HttpRequestMessage AuthedGet(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task<IfoodCategoryDetailResult> GetCategoryDetailAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = AuthedGet(url, accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodCategoryDetailResult(false, null, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            return new IfoodCategoryDetailResult(true, ParseCategory(doc.RootElement), text, null);
        }
        catch (Exception ex)
        {
            return new IfoodCategoryDetailResult(false, null, null, ex.Message);
        }
    }

    private async Task<IfoodProductListResult> ListProductsFromUrlAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = AuthedGet(url, accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodProductListResult(false, [], await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            var list = new List<IfoodProductDto>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                foreach (var el in doc.RootElement.EnumerateArray())
                    list.Add(ParseProduct(el));

            return new IfoodProductListResult(true, list, null);
        }
        catch (Exception ex)
        {
            return new IfoodProductListResult(false, [], ex.Message);
        }
    }

    private async Task<IfoodProductDetailResult> SendProductWriteAsync(HttpMethod method, string url, string accessToken, object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(payload) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IfoodProductDetailResult(false, null, await ErrorMessageAsync(response, cancellationToken));

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(text);
            return new IfoodProductDetailResult(true, ParseProduct(doc.RootElement), null);
        }
        catch (Exception ex)
        {
            return new IfoodProductDetailResult(false, null, ex.Message);
        }
    }

    private static object BuildProductPayload(IfoodUpsertProductRequest request, bool includeId)
    {
        var shifts = (request.Shifts ?? []).Select(s => new
        {
            startTime = s.StartTime, endTime = s.EndTime, monday = s.Monday, tuesday = s.Tuesday, wednesday = s.Wednesday,
            thursday = s.Thursday, friday = s.Friday, saturday = s.Saturday, sunday = s.Sunday,
        }).ToArray();

        return includeId
            ? new
            {
                id = request.Id,
                name = request.Name,
                description = request.Description,
                additionalInformation = request.AdditionalInformation,
                externalCode = request.ExternalCode,
                ean = request.Ean,
                image = request.Image,
                shifts,
            }
            : new
            {
                name = request.Name,
                description = request.Description,
                additionalInformation = request.AdditionalInformation,
                externalCode = request.ExternalCode,
                ean = request.Ean,
                image = request.Image,
                shifts,
            };
    }

    private async Task<IfoodCatalogActionResult> SendActionAsync(HttpMethod method, string url, string accessToken, object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(payload) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IfoodCatalogActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IfoodCatalogActionResult(false, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IfoodCatalogActionResult(false, ex.Message);
        }
    }

    private async Task<IfoodCatalogActionResult> SendDeleteAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IfoodCatalogActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IfoodCatalogActionResult(false, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IfoodCatalogActionResult(false, ex.Message);
        }
    }

    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}";
    }

    private static IfoodCategoryDto ParseCategory(JsonElement el)
    {
        var index = el.TryGetProperty("index", out var iEl) && iEl.ValueKind == JsonValueKind.Number ? (int?)iEl.GetInt32() : null;
        return new IfoodCategoryDto(GetString(el, "id"), index, GetString(el, "name"), GetString(el, "externalCode"), GetString(el, "status"), GetString(el, "template"));
    }

    private static IfoodProductDto ParseProduct(JsonElement el)
    {
        bool? industrialized = el.TryGetProperty("industrialized", out var indEl) && (indEl.ValueKind == JsonValueKind.True || indEl.ValueKind == JsonValueKind.False)
            ? indEl.GetBoolean() : null;
        return new IfoodProductDto(GetString(el, "id"), GetString(el, "name"), GetString(el, "description"), GetString(el, "additionalInformation"), GetString(el, "externalCode"), GetString(el, "ean"), industrialized, GetString(el, "imagePath"));
    }

    private static IfoodOptionGroupDto ParseOptionGroup(JsonElement el)
    {
        var index = el.TryGetProperty("index", out var iEl) && iEl.ValueKind == JsonValueKind.Number ? (int?)iEl.GetInt32() : null;
        return new IfoodOptionGroupDto(GetString(el, "id"), GetString(el, "name"), GetString(el, "externalCode"), GetString(el, "status"), index);
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTime? GetDate(JsonElement element, string propertyName)
    {
        var s = GetString(element, propertyName);
        return s is not null && DateTime.TryParse(s, out var dt) ? dt : null;
    }

    private static string Truncate(string value) => value.Length > 300 ? value[..300] + "…" : value;

    // DTO interno de desserialização — ReadFromJsonAsync sem options explícitas já é
    // case-insensitive por padrão (mesmo padrão usado em IfoodAuthClient/IfoodOrderClient).
    private sealed record CategoryResponseDto(string? Id);

    #endregion
}
