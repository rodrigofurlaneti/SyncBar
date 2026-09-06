using System.Net;
using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodCatalogClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IfoodCatalogClient _client;

    public IfoodCatalogClientTests()
    {
        _client = new IfoodCatalogClient(new HttpClient(_handler));
    }

    private HttpRequestMessage LastRequest => _handler.Requests[^1];
    private string? LastRequestBody => _handler.RequestBodies[^1];

    // ---------------- Fluxo essencial (Fase 3/6a) ----------------

    [Fact]
    public async Task CreateCategoryAsync_Success_ShouldPostToCatalogScopedPathAndReturnId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"cat-1"}""");

        var result = await _client.CreateCategoryAsync("tok", "MERCH-1", "catalog-1", "Bebidas", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.IfoodCategoryId.Should().Be("cat-1");
        LastRequest.RequestUri!.ToString().Should().EndWith("merchants/MERCH-1/catalogs/catalog-1/categories");
        LastRequestBody.Should().Contain("Bebidas").And.Contain("AVAILABLE");
    }

    [Fact]
    public async Task CreateCategoryAsync_ResponseWithoutId_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{}""");

        var result = await _client.CreateCategoryAsync("tok", "MERCH-1", "catalog-1", "Bebidas", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("id");
    }

    [Fact]
    public async Task CreateCategoryAsync_Failure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "catalogo invalido");

        var result = await _client.CreateCategoryAsync("tok", "MERCH-1", "catalog-1", "Bebidas", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("catalogo invalido");
    }

    [Fact]
    public async Task CreateCategoryAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.CreateCategoryAsync("tok", "MERCH-1", "catalog-1", "Bebidas", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    private static IfoodUpsertItemRequest ValidItemRequest(IReadOnlyCollection<IfoodUpsertItemOptionGroup>? optionGroups = null) => new(
        ItemId: Guid.NewGuid(),
        IfoodCategoryId: "cat-1",
        Available: true,
        Price: 25m,
        ExternalCode: "SB-1",
        ProductId: Guid.NewGuid(),
        ProductName: "Pizza",
        ProductDescription: "Descricao",
        ProductExternalCode: "SB-1-P",
        OptionGroups: optionGroups);

    [Fact]
    public async Task UpsertItemAsync_WithoutOptionGroups_ShouldPutItemAndProductWithEmptyOptionGroups()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.UpsertItemAsync("tok", "MERCH-1", ValidItemRequest(), CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.Method.Should().Be(HttpMethod.Put);
        LastRequest.RequestUri!.ToString().Should().EndWith("merchants/MERCH-1/items");
        LastRequestBody.Should().Contain("\"categoryId\":\"cat-1\"").And.Contain("\"optionGroups\":[]");
    }

    [Fact]
    public async Task UpsertItemAsync_WithOptionGroups_ShouldIncludeNestedOptionsInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var optionGroups = new[]
        {
            new IfoodUpsertItemOptionGroup(Guid.NewGuid(), "Adicionais", 0, 1,
                [new IfoodUpsertItemOption(Guid.NewGuid(), Guid.NewGuid(), "Queijo extra", 3m, true)]),
        };

        var result = await _client.UpsertItemAsync("tok", "MERCH-1", ValidItemRequest(optionGroups), CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequestBody.Should().Contain("Adicionais").And.Contain("Queijo extra");
    }

    [Fact]
    public async Task UpsertItemAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "item invalido");

        var result = await _client.UpsertItemAsync("tok", "MERCH-1", ValidItemRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("item invalido");
    }

    [Fact]
    public async Task SetItemStatusAsync_Success_ShouldPatchStatus()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var itemId = Guid.NewGuid();

        var result = await _client.SetItemStatusAsync("tok", "MERCH-1", itemId, true, CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.Method.Should().Be(HttpMethod.Patch);
        LastRequest.RequestUri!.ToString().Should().EndWith("merchants/MERCH-1/items/status");
        LastRequestBody.Should().Contain(itemId.ToString()).And.Contain("AVAILABLE");
    }

    [Fact]
    public async Task SetItemStatusAsync_Unavailable_ShouldSendUnavailableStatus()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        await _client.SetItemStatusAsync("tok", "MERCH-1", Guid.NewGuid(), false, CancellationToken.None);

        LastRequestBody.Should().Contain("UNAVAILABLE");
    }

    // Testes canônicos de SendActionAsync (compartilhado por ~20 métodos deste client) — cobre o
    // caminho de sucesso/falha/exceção uma única vez aqui; os demais wrappers abaixo confirmam
    // apenas o próprio diferencial (URL/payload), já que reusam exatamente esta mesma lógica.
    [Fact]
    public async Task SetItemStatusAsync_Failure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "item nao encontrado");

        var result = await _client.SetItemStatusAsync("tok", "MERCH-1", Guid.NewGuid(), true, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("item nao encontrado");
    }

    [Fact]
    public async Task SetItemStatusAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.SetItemStatusAsync("tok", "MERCH-1", Guid.NewGuid(), true, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task SetInventoryAsync_ShouldPostProductIdAndAmount()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var productId = Guid.NewGuid();

        var result = await _client.SetInventoryAsync("tok", "MERCH-1", productId, 42, CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.Method.Should().Be(HttpMethod.Post);
        LastRequest.RequestUri!.ToString().Should().EndWith("merchants/MERCH-1/inventory");
        LastRequestBody.Should().Contain(productId.ToString()).And.Contain("42");
    }

    // ---------------- Catalogs / Categories / Sellable items (v2) ----------------

    [Fact]
    public async Task GetCatalogsAsync_Success_ShouldMapCatalogsWithContext()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            [{"catalogId":"cat-1","status":"AVAILABLE","context":["DELIVERY","TAKEOUT"],"groupId":"grp-1","modifiedAt":"2026-01-01T00:00:00Z"}]
            """);

        var result = await _client.GetCatalogsAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Catalogs.Should().ContainSingle(c => c.CatalogId == "cat-1" && c.Context!.Contains("DELIVERY"));
    }

    [Fact]
    public async Task GetCatalogsAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetCatalogsAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Catalogs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCatalogsAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetCatalogsAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task ListCategoriesAsync_ArrayResponse_ShouldMapAllCategories()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"cat-1","name":"Bebidas","index":0,"status":"AVAILABLE"}]""");

        var result = await _client.ListCategoriesAsync("tok", "MERCH-1", "catalog-1", true, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Categories.Should().ContainSingle(c => c.Id == "cat-1" && c.Name == "Bebidas");
        LastRequest.RequestUri!.ToString().Should().Contain("includeItems=true");
    }

    [Fact]
    public async Task ListCategoriesAsync_SingleObjectResponse_ShouldWrapIntoOneItemList()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"cat-1","name":"Bebidas"}""");

        var result = await _client.ListCategoriesAsync("tok", "MERCH-1", "catalog-1", cancellationToken: CancellationToken.None);

        result.Categories.Should().ContainSingle(c => c.Id == "cat-1");
    }

    [Fact]
    public async Task ListCategoriesAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.ListCategoriesAsync("tok", "MERCH-1", "catalog-1", cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetCategoryAsync_Success_ShouldReturnMappedCategoryAndRawPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"cat-1","name":"Bebidas"}""");

        var result = await _client.GetCategoryAsync("tok", "MERCH-1", "catalog-1", "cat-1", cancellationToken: CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Category!.Name.Should().Be("Bebidas");
        result.RawPayload.Should().Contain("Bebidas");
        LastRequest.RequestUri!.ToString().Should().EndWith("categories/cat-1?includeItems=false");
    }

    [Fact]
    public async Task GetCategoryAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao encontrado");

        var result = await _client.GetCategoryAsync("tok", "MERCH-1", "catalog-1", "cat-missing", cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetCategoryAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetCategoryAsync("tok", "MERCH-1", "catalog-1", "cat-1", cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task EditCategoryAsync_OnlySomeFieldsProvided_ShouldOnlyIncludeThoseInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"cat-1","name":"Novo nome"}""");

        var result = await _client.EditCategoryAsync("tok", "MERCH-1", "catalog-1", "cat-1", "Novo nome", null, null, null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Category!.Name.Should().Be("Novo nome");
        LastRequest.Method.Should().Be(HttpMethod.Patch);
        LastRequestBody.Should().Contain("Novo nome");
        LastRequestBody.Should().NotContain("externalCode").And.NotContain("status").And.NotContain("index");
    }

    [Fact]
    public async Task EditCategoryAsync_AllFieldsProvided_ShouldIncludeAllInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"cat-1"}""");

        await _client.EditCategoryAsync("tok", "MERCH-1", "catalog-1", "cat-1", "Nome", "ext-1", "AVAILABLE", 2, CancellationToken.None);

        LastRequestBody.Should().Contain("Nome").And.Contain("ext-1").And.Contain("AVAILABLE").And.Contain("2");
    }

    [Fact]
    public async Task EditCategoryAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "nome invalido");

        var result = await _client.EditCategoryAsync("tok", "MERCH-1", "catalog-1", "cat-1", "x", null, null, null, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task EditCategoryAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.EditCategoryAsync("tok", "MERCH-1", "catalog-1", "cat-1", "x", null, null, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task DeleteCategoryAsync_Success_ShouldSendDeleteWithoutCatalogIdInPath()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.DeleteCategoryAsync("tok", "MERCH-1", "cat-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.Method.Should().Be(HttpMethod.Delete);
        LastRequest.RequestUri!.ToString().Should().EndWith("merchants/MERCH-1/categories/cat-1");
    }

    [Fact]
    public async Task DeleteCategoryAsync_Failure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "categoria com itens");

        var result = await _client.DeleteCategoryAsync("tok", "MERCH-1", "cat-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("categoria com itens");
    }

    [Fact]
    public async Task DeleteCategoryAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.DeleteCategoryAsync("tok", "MERCH-1", "cat-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task ListSellableItemsAsync_Success_ShouldMapPriceFromNestedValue()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"itemId":"item-1","categoryId":"cat-1","itemName":"Pizza","itemPrice":{"value":30}}]""");

        var result = await _client.ListSellableItemsAsync("tok", "MERCH-1", "grp-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Items.Should().ContainSingle(i => i.ItemId == "item-1" && i.ItemPriceValue == 30);
    }

    [Fact]
    public async Task ListSellableItemsAsync_WithoutPrice_ShouldReturnNullPrice()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"itemId":"item-1"}]""");

        var result = await _client.ListSellableItemsAsync("tok", "MERCH-1", "grp-1", CancellationToken.None);

        result.Items.Single().ItemPriceValue.Should().BeNull();
    }

    [Fact]
    public async Task ListSellableItemsAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.ListSellableItemsAsync("tok", "MERCH-1", "grp-1", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    // ---------------- Items (v2 — flat) ----------------

    [Fact]
    public async Task GetItemFlatAsync_ItemWrappedInRootProperty_ShouldUnwrapAndMap()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"item":{"id":"item-1","status":"AVAILABLE","price":{"value":15},"externalCode":"ext-1","categoryId":"cat-1"}}""");

        var result = await _client.GetItemFlatAsync("tok", "MERCH-1", Guid.NewGuid(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ItemId.Should().Be("item-1");
        result.PriceValue.Should().Be(15);
    }

    [Fact]
    public async Task GetItemFlatAsync_ItemAtRoot_ShouldMapDirectly()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"item-1","status":"AVAILABLE"}""");

        var result = await _client.GetItemFlatAsync("tok", "MERCH-1", Guid.NewGuid(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ItemId.Should().Be("item-1");
    }

    [Fact]
    public async Task GetItemFlatAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao encontrado");

        var result = await _client.GetItemFlatAsync("tok", "MERCH-1", Guid.NewGuid(), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SetItemPriceAsync_WithPriceByCatalog_ShouldIncludeItInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var itemId = Guid.NewGuid();

        var result = await _client.SetItemPriceAsync("tok", "MERCH-1", itemId, 20m, 25m, [new IfoodItemPriceByCatalog(18m, "MARKETPLACE")], CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().EndWith("merchants/MERCH-1/items/price");
        LastRequestBody.Should().Contain("MARKETPLACE").And.Contain("18");
    }

    [Fact]
    public async Task SetItemExternalCodeAsync_ShouldPatchExternalCode()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.SetItemExternalCodeAsync("tok", "MERCH-1", Guid.NewGuid(), "new-ext", cancellationToken: CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequestBody.Should().Contain("new-ext");
    }

    [Fact]
    public async Task DeleteItemAsync_WithCatalogContext_ShouldIncludeQueryParam()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var productId = Guid.NewGuid();

        var result = await _client.DeleteItemAsync("tok", "MERCH-1", "cat-1", productId, "MARKETPLACE", CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().Contain($"categories/cat-1/products/{productId}").And.Contain("catalogContext=MARKETPLACE");
    }

    [Fact]
    public async Task DeleteItemAsync_WithoutCatalogContext_ShouldOmitQueryString()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        await _client.DeleteItemAsync("tok", "MERCH-1", "cat-1", Guid.NewGuid(), cancellationToken: CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().NotContain("?");
    }

    [Fact]
    public async Task ListCategoryItemsAsync_Success_ShouldReturnRawPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"items":[{"id":"item-1"}]}""");

        var result = await _client.ListCategoryItemsAsync("tok", "MERCH-1", "cat-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RawPayload.Should().Contain("item-1");
    }

    [Fact]
    public async Task ListCategoryItemsAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.ListCategoryItemsAsync("tok", "MERCH-1", "cat-1", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    // ---------------- Products (v2) ----------------

    [Fact]
    public async Task ListProductsAsync_WithLimitAndPage_ShouldIncludeBothInQuery()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"prod-1","name":"Pizza"}]""");

        var result = await _client.ListProductsAsync("tok", "MERCH-1", 50, 2, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Products.Should().ContainSingle(p => p.Id == "prod-1");
        LastRequest.RequestUri!.ToString().Should().Contain("limit=50&page=2");
    }

    [Fact]
    public async Task ListProductsAsync_WithoutFilters_ShouldOmitQueryString()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[]""");

        await _client.ListProductsAsync("tok", "MERCH-1", cancellationToken: CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().NotContain("?");
    }

    [Fact]
    public async Task ListProductsAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.ListProductsAsync("tok", "MERCH-1", cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    private static IfoodUpsertProductRequest ValidProductRequest(string? id = "prod-1") => new(
        Id: id, Name: "Pizza", Description: "Descricao", AdditionalInformation: null,
        ExternalCode: "ext-1", Ean: null, Image: null);

    [Fact]
    public async Task CreateProductAsync_ShouldIncludeIdInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"prod-1","name":"Pizza"}""");

        var result = await _client.CreateProductAsync("tok", "MERCH-1", ValidProductRequest(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Product!.Name.Should().Be("Pizza");
        LastRequest.Method.Should().Be(HttpMethod.Post);
        LastRequestBody.Should().Contain("\"id\":\"prod-1\"");
    }

    [Fact]
    public async Task CreateProductAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "produto invalido");

        var result = await _client.CreateProductAsync("tok", "MERCH-1", ValidProductRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProductAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.CreateProductAsync("tok", "MERCH-1", ValidProductRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task EditProductAsync_ShouldOmitIdFromPayloadAndUsePut()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"prod-1","name":"Pizza Editada"}""");
        var productId = Guid.NewGuid();

        var result = await _client.EditProductAsync("tok", "MERCH-1", productId, ValidProductRequest(), CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.Method.Should().Be(HttpMethod.Put);
        LastRequest.RequestUri!.ToString().Should().EndWith($"products/{productId}");
        LastRequestBody.Should().NotContain("\"id\":");
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldSendDeleteToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var productId = Guid.NewGuid();

        var result = await _client.DeleteProductAsync("tok", "MERCH-1", productId, CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().EndWith($"products/{productId}");
    }

    [Fact]
    public async Task BatchUpdateProductStatusesAsync_WithCatalogContext_ShouldIncludeQueryAndPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var items = new[] { new IfoodBatchProductStatusItem("prod-1", null, "AVAILABLE") };

        var result = await _client.BatchUpdateProductStatusesAsync("tok", "MERCH-1", items, "MARKETPLACE", CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().Contain("catalogContext=MARKETPLACE");
        LastRequestBody.Should().Contain("prod-1").And.Contain("AVAILABLE");
    }

    [Fact]
    public async Task BatchUpdateProductPricesAsync_SuccessWithBody_ShouldReturnUrlAndBatchId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"url":"https://batch.example/1","batchId":"batch-1"}""");
        var items = new[] { new IfoodBatchProductPriceItem("prod-1", null, 10m) };

        var result = await _client.BatchUpdateProductPricesAsync("tok", "MERCH-1", items, cancellationToken: CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Url.Should().Be("https://batch.example/1");
        result.BatchId.Should().Be("batch-1");
    }

    [Fact]
    public async Task BatchUpdateProductPricesAsync_SuccessWithEmptyBody_ShouldReturnSuccessWithNullFields()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var items = new[] { new IfoodBatchProductPriceItem("prod-1", null, 10m) };

        var result = await _client.BatchUpdateProductPricesAsync("tok", "MERCH-1", items, cancellationToken: CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Url.Should().BeNull();
        result.BatchId.Should().BeNull();
    }

    [Fact]
    public async Task BatchUpdateProductPricesAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "preco invalido");
        var items = new[] { new IfoodBatchProductPriceItem("prod-1", null, -1m) };

        var result = await _client.BatchUpdateProductPricesAsync("tok", "MERCH-1", items, cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task BatchUpdateProductPricesAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));
        var items = new[] { new IfoodBatchProductPriceItem("prod-1", null, 10m) };

        var result = await _client.BatchUpdateProductPricesAsync("tok", "MERCH-1", items, cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task ListProductsByExternalCodeAsync_ShouldEscapeExternalCodeInPath()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"prod-1","externalCode":"ext/1"}]""");

        var result = await _client.ListProductsByExternalCodeAsync("tok", "MERCH-1", "ext/1", CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().Contain("products/externalCode/ext%2F1");
    }

    [Fact]
    public async Task GetProductByIdAsync_Success_ShouldMapProduct()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"prod-1","name":"Pizza","industrialized":false}""");
        var productId = Guid.NewGuid();

        var result = await _client.GetProductByIdAsync("tok", "MERCH-1", productId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Product!.Name.Should().Be("Pizza");
        result.Product.Industrialized.Should().BeFalse();
        LastRequest.RequestUri!.ToString().Should().EndWith($"product/{productId}");
    }

    [Fact]
    public async Task GetProductByIdAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao encontrado");

        var result = await _client.GetProductByIdAsync("tok", "MERCH-1", Guid.NewGuid(), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    // ---------------- Option groups / Options (v2 — manutenção) ----------------

    [Fact]
    public async Task ListOptionGroupsAsync_ArrayResponseWithCatalogContext_ShouldMapGroupsAndIncludeQuery()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"og-1","name":"Adicionais"}]""");

        var result = await _client.ListOptionGroupsAsync("tok", "MERCH-1", true, "MARKETPLACE", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OptionGroups.Should().ContainSingle(g => g.Id == "og-1");
        LastRequest.RequestUri!.ToString().Should().Contain("includeOptions=true").And.Contain("catalogContext=MARKETPLACE");
    }

    [Fact]
    public async Task ListOptionGroupsAsync_SingleObjectResponse_ShouldWrapIntoOneItemList()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"og-1","name":"Adicionais"}""");

        var result = await _client.ListOptionGroupsAsync("tok", "MERCH-1", cancellationToken: CancellationToken.None);

        result.OptionGroups.Should().ContainSingle(g => g.Id == "og-1");
    }

    [Fact]
    public async Task ListOptionGroupsAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.ListOptionGroupsAsync("tok", "MERCH-1", cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOptionGroupAsync_ShouldPatchName()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.UpdateOptionGroupAsync("tok", "MERCH-1", Guid.NewGuid(), "Novo nome", CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequestBody.Should().Contain("Novo nome");
    }

    [Fact]
    public async Task DeleteOptionGroupAsync_ShouldSendDeleteToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var groupId = Guid.NewGuid();

        var result = await _client.DeleteOptionGroupAsync("tok", "MERCH-1", groupId, CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().EndWith($"optionGroups/{groupId}");
    }

    [Fact]
    public async Task DisassociateOptionGroupFromProductAsync_ShouldSendDeleteToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var groupId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var result = await _client.DisassociateOptionGroupFromProductAsync("tok", "MERCH-1", groupId, productId, CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().EndWith($"optionGroups/{groupId}/products/{productId}");
    }

    [Fact]
    public async Task DeleteOptionAsync_WithCatalogContext_ShouldIncludeQueryParam()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.DeleteOptionAsync("tok", "MERCH-1", Guid.NewGuid(), Guid.NewGuid(), "MARKETPLACE", CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().Contain("/option?catalogContext=MARKETPLACE");
    }

    [Fact]
    public async Task UpdateOptionGroupStatusAsync_ShouldPatchAvailability()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.UpdateOptionGroupStatusAsync("tok", "MERCH-1", Guid.NewGuid(), false, CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequestBody.Should().Contain("UNAVAILABLE");
    }

    [Fact]
    public async Task SetOptionPriceAsync_ShouldPatchPriceAndParentCustomization()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.SetOptionPriceAsync("tok", "MERCH-1", Guid.NewGuid(), 5m, 8m, "parent-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequestBody.Should().Contain("parent-1");
    }

    [Fact]
    public async Task SetOptionExternalCodeAsync_ShouldPatchExternalCode()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.SetOptionExternalCodeAsync("tok", "MERCH-1", Guid.NewGuid(), "ext-opt-1", cancellationToken: CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequestBody.Should().Contain("ext-opt-1");
    }

    [Fact]
    public async Task SetOptionStatusAsync_ShouldPatchAvailability()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.SetOptionStatusAsync("tok", "MERCH-1", Guid.NewGuid(), true, cancellationToken: CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequestBody.Should().Contain("AVAILABLE");
    }

    // ---------------- Inventory / Batch results (v2) ----------------

    [Fact]
    public async Task GetInventoryAsync_Success_ShouldMapAmount()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"productId":"prod-1","ownerId":"MERCH-1","amount":7}""");

        var result = await _client.GetInventoryAsync("tok", "MERCH-1", Guid.NewGuid(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Inventory!.Amount.Should().Be(7);
    }

    [Fact]
    public async Task GetInventoryAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao encontrado");

        var result = await _client.GetInventoryAsync("tok", "MERCH-1", Guid.NewGuid(), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteInventoryBatchAsync_ShouldPostAllProductIds()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var result = await _client.DeleteInventoryBatchAsync("tok", "MERCH-1", ids, CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequestBody.Should().Contain(ids[0].ToString()).And.Contain(ids[1].ToString());
    }

    [Fact]
    public async Task GetBatchResultAsync_Success_ShouldMapResultsList()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"batchStatus":"COMPLETED","results":[{"resourceId":"r-1","result":"SUCCESS"}]}""");

        var result = await _client.GetBatchResultAsync("tok", "MERCH-1", "batch-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.BatchStatus.Should().Be("COMPLETED");
        result.Results.Should().ContainSingle(r => r.ResourceId == "r-1" && r.Result == "SUCCESS");
    }

    [Fact]
    public async Task GetBatchResultAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao encontrado");

        var result = await _client.GetBatchResultAsync("tok", "MERCH-1", "batch-missing", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Results.Should().BeEmpty();
    }

    // ---------------- Version (v2) ----------------

    [Fact]
    public async Task CheckVersionAsync_QuotedStringResponse_ShouldTrimQuotes()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "\"v2\"");

        var result = await _client.CheckVersionAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Version.Should().Be("v2");
    }

    [Fact]
    public async Task CheckVersionAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.CheckVersionAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CheckVersionAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.CheckVersionAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task UpgradeVersionAsync_WithCleanMigration_ShouldIncludeQueryParam()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.UpgradeVersionAsync("tok", "MERCH-1", true, CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().Contain("cleanMigration=true");
    }

    [Fact]
    public async Task UpgradeVersionAsync_WithoutCleanMigration_ShouldOmitQueryParam()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        await _client.UpgradeVersionAsync("tok", "MERCH-1", cancellationToken: CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().NotContain("?");
    }

    [Fact]
    public async Task DowngradeVersionAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.DowngradeVersionAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        LastRequest.RequestUri!.ToString().Should().EndWith("version/downgrade");
    }

    // ---------------- Image (v2) ----------------

    [Fact]
    public async Task UploadImageAsync_Success_ShouldSendRawJsonAndReturnRawResponse()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"imagePath":"/images/1.png"}""");

        var result = await _client.UploadImageAsync("tok", "MERCH-1", """{"image":"base64data"}""", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RawPayload.Should().Contain("imagePath");
        LastRequestBody.Should().Be("""{"image":"base64data"}""");
    }

    [Fact]
    public async Task UploadImageAsync_Failure_ShouldReturnFailureWithRawBodyAndErrorMessage()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "imagem invalida");

        var result = await _client.UploadImageAsync("tok", "MERCH-1", "{}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.RawPayload.Should().Be("imagem invalida");
        result.ErrorMessage.Should().Contain("400");
    }

    [Fact]
    public async Task UploadImageAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.UploadImageAsync("tok", "MERCH-1", "{}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    // ---------------- Catálogo v1 (legado) — despachante genérico ----------------

    [Fact]
    public async Task InvokeCatalogV1Async_SimpleGetOperation_ShouldBuildCorrectUrlAndIgnoreBody()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"cat-1"}]""");

        var result = await _client.InvokeCatalogV1Async("tok", "MERCH-1", IfoodCatalogV1Operation.ListCatalogs, jsonBody: "{\"ignored\":true}", cancellationToken: CancellationToken.None);

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        LastRequest.RequestUri!.ToString().Should().EndWith("catalog/v1.0/merchants/MERCH-1/catalogs");
        LastRequest.Content.Should().BeNull();
    }

    [Fact]
    public async Task InvokeCatalogV1Async_OperationWithRouteParams_ShouldSubstituteThemInPath()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        var routeParams = new Dictionary<string, string> { ["catalogId"] = "catalog-1", ["categoryId"] = "cat-1" };

        await _client.InvokeCatalogV1Async("tok", "MERCH-1", IfoodCatalogV1Operation.EditCategory, routeParams, jsonBody: "{}", cancellationToken: CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().EndWith("catalogs/catalog-1/categories/cat-1");
        LastRequest.Method.Should().Be(HttpMethod.Patch);
        LastRequestBody.Should().Be("{}");
    }

    [Fact]
    public async Task InvokeCatalogV1Async_MissingRouteParam_ShouldReturnFailureWithoutCallingHttp()
    {
        var result = await _client.InvokeCatalogV1Async("tok", "MERCH-1", IfoodCatalogV1Operation.EditCategory, cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(0);
        result.ErrorMessage.Should().Contain("Faltam parâmetros");
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeCatalogV1Async_WithQueryParams_ShouldAppendThemToUrl()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        var routeParams = new Dictionary<string, string> { ["itemId"] = "item-1" };
        var queryParams = new Dictionary<string, string> { ["page"] = "2" };

        await _client.InvokeCatalogV1Async("tok", "MERCH-1", IfoodCatalogV1Operation.GetItem, routeParams, queryParams, cancellationToken: CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().Contain("items/item-1?page=2");
    }

    [Fact]
    public async Task InvokeCatalogV1Async_PostOperationWithBody_ShouldSendJsonContent()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.InvokeCatalogV1Async("tok", "MERCH-1", IfoodCatalogV1Operation.CreateProduct, jsonBody: """{"name":"Pizza"}""", cancellationToken: CancellationToken.None);

        LastRequest.Method.Should().Be(HttpMethod.Post);
        LastRequestBody.Should().Be("""{"name":"Pizza"}""");
    }

    [Fact]
    public async Task InvokeCatalogV1Async_DeleteOperationWithBody_ShouldNotSendContentEvenIfProvided()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        var routeParams = new Dictionary<string, string> { ["optionGroupId"] = Guid.NewGuid().ToString() };

        await _client.InvokeCatalogV1Async("tok", "MERCH-1", IfoodCatalogV1Operation.DeleteOptionGroup, routeParams, jsonBody: "{\"ignored\":true}", cancellationToken: CancellationToken.None);

        LastRequest.Method.Should().Be(HttpMethod.Delete);
        LastRequest.Content.Should().BeNull();
    }

    [Fact]
    public async Task InvokeCatalogV1Async_HttpFailure_ShouldStillReturnSuccessFalseWithRawBodyAndStatus()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "erro de validacao");

        var result = await _client.InvokeCatalogV1Async("tok", "MERCH-1", IfoodCatalogV1Operation.ListCatalogs, cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ResponseBody.Should().Be("erro de validacao");
        result.ErrorMessage.Should().Contain("400").And.Contain("erro de validacao");
    }

    [Fact]
    public async Task InvokeCatalogV1Async_NetworkException_ShouldReturnFailureWithZeroStatusCode()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.InvokeCatalogV1Async("tok", "MERCH-1", IfoodCatalogV1Operation.ListCatalogs, cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(0);
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task InvokeCatalogV1Async_MerchantIdWithSpecialCharacters_ShouldEscapeInPath()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "[]");

        await _client.InvokeCatalogV1Async("tok", "MERCH/1", IfoodCatalogV1Operation.ListCatalogs, cancellationToken: CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().Contain("MERCH%2F1");
    }
}
