using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Analytics;
using SyncBar.Application.Features.Integrations.IFood.Catalog;
using SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;
using SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;
using SyncBar.Application.Features.Integrations.IFood.Catalog.Items;
using SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;
using SyncBar.Application.Features.Integrations.IFood.Catalog.Products;
using SyncBar.Application.Features.Integrations.IFood.Catalog.V1Legacy;
using SyncBar.Application.Features.Integrations.IFood.Financial;
using SyncBar.Application.Features.Integrations.IFood.Logistics;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Application.Features.Integrations.IFood.Review;
using SyncBar.Application.Features.Integrations.IFood.Shipping;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Roles = "Administrador,Gerente")]
public sealed class IntegrationsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("ifood/company/{companyId:long}")]
    public Task<IActionResult> GetIFoodSettings(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodSettings), async () =>
        {
            var result = await Mediator.Send(new GetIFoodSettingsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood")]
    public Task<IActionResult> SaveIFoodSettings([FromBody] SaveIFoodSettingsCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SaveIFoodSettings), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/test-connection")]
    public Task<IActionResult> TestIFoodConnection([FromBody] TestIFoodConnectionCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(TestIFoodConnection), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/merchants/company/{companyId:long}")]
    public Task<IActionResult> GetIFoodMerchantMappings(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodMerchantMappings), async () =>
        {
            var result = await Mediator.Send(new GetIFoodMerchantMappingsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood/merchants")]
    public Task<IActionResult> SetIFoodMerchantMapping([FromBody] SetIFoodMerchantMappingCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodMerchantMapping), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Pedidos iFood ("fluxo essencial", fase 2) — a sincronização em si roda sozinha em segundo
    // plano (IFoodOrderPollingBackgroundService); estes endpoints são só pra tela acompanhar e
    // avançar o status manualmente (confirmar já é automático).
    [HttpGet("ifood/orders/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodOrders(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOrders), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOrdersQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/start-preparation")]
    public Task<IActionResult> StartIFoodOrderPreparation(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(StartIFoodOrderPreparation), async () =>
        {
            var result = await Mediator.Send(new StartIFoodOrderPreparationCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/ready")]
    public Task<IActionResult> MarkIFoodOrderReady(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIFoodOrderReady), async () =>
        {
            var result = await Mediator.Send(new MarkIFoodOrderReadyCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/orders/{ifoodOrderId:long}/cancellation-reasons")]
    public Task<IActionResult> GetIFoodCancellationReasons(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodCancellationReasons), async () =>
        {
            var result = await Mediator.Send(new GetIFoodCancellationReasonsQuery(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/cancel")]
    public Task<IActionResult> CancelIFoodOrder(long ifoodOrderId, [FromBody] CancelIFoodOrderRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIFoodOrder), async () =>
        {
            var result = await Mediator.Send(new CancelIFoodOrderCommand(ifoodOrderId, request.ReasonCode), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Fase 9b — rastreamento do entregador e código de retirada do módulo Order (pedidos que
    // vieram do iFood), mais aceite/rejeição de disputas Handshake informadas manualmente pela
    // equipe (ver ressalva em AcceptIFoodDisputeCommand).
    [HttpGet("ifood/orders/{ifoodOrderId:long}/tracking")]
    public Task<IActionResult> GetIFoodOrderTracking(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOrderTracking), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOrderTrackingQuery(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/validate-pickup-code")]
    public Task<IActionResult> ValidateIFoodPickupCode(long ifoodOrderId, [FromBody] ValidateIFoodPickupCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ValidateIFoodPickupCode), async () =>
        {
            var result = await Mediator.Send(new ValidateIFoodPickupCodeCommand(ifoodOrderId, request.Code), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { codeMatched = result.Value });
        });

    [HttpPost("ifood/disputes/{disputeId}/accept")]
    public Task<IActionResult> AcceptIFoodDispute(string disputeId, [FromBody] IFoodDisputeActionRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AcceptIFoodDispute), async () =>
        {
            var result = await Mediator.Send(new AcceptIFoodDisputeCommand(request.BranchId, disputeId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/disputes/{disputeId}/reject")]
    public Task<IActionResult> RejectIFoodDispute(string disputeId, [FromBody] RejectIFoodDisputeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RejectIFoodDispute), async () =>
        {
            var result = await Mediator.Send(new RejectIFoodDisputeCommand(request.BranchId, disputeId, request.Reason), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Fase 9c — fecha os gaps restantes do módulo Order da auditoria de 2026-08-20: proposta de
    // alternativa em disputa, virtual bag e requestDriver/cancelRequestDriver/verifyDeliveryCode
    // do PRÓPRIO módulo Order (distintos dos homônimos em Shipping/Logistics já implementados —
    // ver comentário em IIFoodOrderClient).
    [HttpPost("ifood/disputes/{disputeId}/alternatives/{alternativeId}")]
    public Task<IActionResult> RequestIFoodDisputeAlternative(
        string disputeId, string alternativeId, [FromBody] RequestIFoodDisputeAlternativeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodDisputeAlternative), async () =>
        {
            var result = await Mediator.Send(new RequestIFoodDisputeAlternativeCommand(
                request.BranchId, disputeId, alternativeId, request.AlternativeType, request.Amount, request.Currency), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/orders/{ifoodOrderId:long}/virtual-bag")]
    public Task<IActionResult> GetIFoodOrderVirtualBag(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOrderVirtualBag), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOrderVirtualBagQuery(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/request-driver")]
    public Task<IActionResult> RequestIFoodOrderDriver(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodOrderDriver), async () =>
        {
            var result = await Mediator.Send(new RequestIFoodOrderDriverCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/cancel-request-driver")]
    public Task<IActionResult> CancelIFoodOrderDriverRequest(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIFoodOrderDriverRequest), async () =>
        {
            var result = await Mediator.Send(new CancelIFoodOrderDriverRequestCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/verify-delivery-code")]
    public Task<IActionResult> VerifyIFoodOrderDeliveryCode(long ifoodOrderId, [FromBody] VerifyIFoodOrderDeliveryCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(VerifyIFoodOrderDeliveryCode), async () =>
        {
            var result = await Mediator.Send(new VerifyIFoodOrderDeliveryCodeCommand(ifoodOrderId, request.Code), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { codeMatched = result.Value });
        });

    // Cardápio iFood ("fluxo essencial", fase 3) — assim como os pedidos, a sincronização roda
    // sozinha (disparada automaticamente a cada produto/categoria criado/editado/desativado, ver
    // IIFoodCatalogSyncTrigger); este endpoint é só o botão "Sincronizar agora" da tela, pra
    // reenviar tudo de uma vez (carga inicial ou recuperação de falha).
    [HttpPost("ifood/catalog/sync")]
    public Task<IActionResult> SyncIFoodCatalog([FromBody] SyncIFoodCatalogRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SyncIFoodCatalog), async () =>
        {
            var result = await Mediator.Send(new SyncIFoodCatalogCommand(request.CompanyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Fase 10 — módulo Catalog completo. Tier 1 (v2, versão viva, já usada pela sincronização
    // automática desde a fase 3): CRUD tipado dedicado pra catálogos/categorias, produtos, itens
    // (formato "flat" v2), grupos de opções/opções e operações administrativas (estoque, lote,
    // versão, imagem). Tier 2 (v1, legado): um único endpoint despachante genérico — ver
    // InvokeIFoodCatalogV1Operation — que cobre os 56 endpoints da v1 sem duplicar tipagem pra uma
    // API que nenhum merchant do SyncBar usa hoje (todo merchant está em v1 OU v2, nunca nos dois).
    // Ressalva importante: os nomes de campo abaixo foram confirmados contra a collection oficial
    // do Postman (iFood), mas os VALORES de exemplo da doc são placeholders gerados pelo Postman
    // (schema mock), não tráfego real capturado — tratar como "estrutura confirmada, valores não
    // confirmados" até testar contra o sandbox.

    // --- Categories --------------------------------------------------------------------------

    [HttpGet("ifood/catalog/branch/{branchId:long}/catalogs")]
    public Task<IActionResult> GetIFoodCatalogs(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodCatalogs), async () =>
        {
            var result = await Mediator.Send(new GetIFoodCatalogsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/catalog/branch/{branchId:long}/catalogs/{catalogId}/categories")]
    public Task<IActionResult> ListIFoodCategories(long branchId, string catalogId, [FromQuery] bool includeItems, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIFoodCategories), async () =>
        {
            var result = await Mediator.Send(new ListIFoodCategoriesQuery(branchId, catalogId, includeItems), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/catalog/branch/{branchId:long}/catalogs/{catalogId}/categories/{categoryId}")]
    public Task<IActionResult> GetIFoodCategory(long branchId, string catalogId, string categoryId, [FromQuery] bool includeItems, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodCategory), async () =>
        {
            var result = await Mediator.Send(new GetIFoodCategoryQuery(branchId, catalogId, categoryId, includeItems), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/catalog/branch/{branchId:long}/catalogs/{catalogId}/categories")]
    public Task<IActionResult> CreateIFoodCategory(long branchId, string catalogId, [FromBody] CreateIFoodCategoryRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CreateIFoodCategory), async () =>
        {
            var result = await Mediator.Send(new CreateIFoodCategoryCommand(branchId, catalogId, request.Name), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood/catalog/branch/{branchId:long}/catalogs/{catalogId}/categories/{categoryId}")]
    public Task<IActionResult> EditIFoodCategory(long branchId, string catalogId, string categoryId, [FromBody] EditIFoodCategoryRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(EditIFoodCategory), async () =>
        {
            var result = await Mediator.Send(new EditIFoodCategoryCommand(branchId, catalogId, categoryId, request.Name, request.ExternalCode, request.Status, request.Index), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpDelete("ifood/catalog/branch/{branchId:long}/categories/{categoryId}")]
    public Task<IActionResult> DeleteIFoodCategory(long branchId, string categoryId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIFoodCategory), async () =>
        {
            var result = await Mediator.Send(new DeleteIFoodCategoryCommand(branchId, categoryId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/catalog/branch/{branchId:long}/sellable-items")]
    public Task<IActionResult> ListIFoodSellableItems(long branchId, [FromQuery] string groupId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIFoodSellableItems), async () =>
        {
            var result = await Mediator.Send(new ListIFoodSellableItemsQuery(branchId, groupId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // --- Products ------------------------------------------------------------------------------

    [HttpGet("ifood/catalog/branch/{branchId:long}/products")]
    public Task<IActionResult> ListIFoodProducts(long branchId, [FromQuery] int? limit, [FromQuery] int? page, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIFoodProducts), async () =>
        {
            var result = await Mediator.Send(new ListIFoodProductsQuery(branchId, limit, page), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/catalog/branch/{branchId:long}/products")]
    public Task<IActionResult> CreateIFoodProduct(long branchId, [FromBody] CreateIFoodProductRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CreateIFoodProduct), async () =>
        {
            var result = await Mediator.Send(new CreateIFoodProductCommand(
                branchId, request.Id, request.Name, request.Description, request.AdditionalInformation,
                request.ExternalCode, request.Ean, request.Image, request.Shifts), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood/catalog/branch/{branchId:long}/products/{productId:guid}")]
    public Task<IActionResult> EditIFoodProduct(long branchId, Guid productId, [FromBody] EditIFoodProductRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(EditIFoodProduct), async () =>
        {
            var result = await Mediator.Send(new EditIFoodProductCommand(
                branchId, productId, request.Name, request.Description, request.AdditionalInformation,
                request.ExternalCode, request.Ean, request.Image, request.Shifts), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpDelete("ifood/catalog/branch/{branchId:long}/products/{productId:guid}")]
    public Task<IActionResult> DeleteIFoodProduct(long branchId, Guid productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIFoodProduct), async () =>
        {
            var result = await Mediator.Send(new DeleteIFoodProductCommand(branchId, productId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPatch("ifood/catalog/branch/{branchId:long}/products/status")]
    public Task<IActionResult> BatchUpdateIFoodProductStatuses(long branchId, [FromBody] BatchUpdateIFoodProductStatusesRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(BatchUpdateIFoodProductStatuses), async () =>
        {
            var result = await Mediator.Send(new BatchUpdateIFoodProductStatusesCommand(branchId, request.Items, request.CatalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/catalog/branch/{branchId:long}/products/price")]
    public Task<IActionResult> BatchUpdateIFoodProductPrices(long branchId, [FromBody] BatchUpdateIFoodProductPricesRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(BatchUpdateIFoodProductPrices), async () =>
        {
            var result = await Mediator.Send(new BatchUpdateIFoodProductPricesCommand(branchId, request.Items, request.CatalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/catalog/branch/{branchId:long}/products/externalCode/{externalCode}")]
    public Task<IActionResult> ListIFoodProductsByExternalCode(long branchId, string externalCode, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIFoodProductsByExternalCode), async () =>
        {
            var result = await Mediator.Send(new ListIFoodProductsByExternalCodeQuery(branchId, externalCode), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/catalog/branch/{branchId:long}/products/{productId:guid}")]
    public Task<IActionResult> GetIFoodProductById(long branchId, Guid productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodProductById), async () =>
        {
            var result = await Mediator.Send(new GetIFoodProductByIdQuery(branchId, productId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // --- Items (v2 — flat) ----------------------------------------------------------------------

    [HttpGet("ifood/catalog/branch/{branchId:long}/items/{itemId:guid}")]
    public Task<IActionResult> GetIFoodItemFlat(long branchId, Guid itemId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodItemFlat), async () =>
        {
            var result = await Mediator.Send(new GetIFoodItemFlatQuery(branchId, itemId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood/catalog/branch/{branchId:long}/items/{itemId:guid}/price")]
    public Task<IActionResult> SetIFoodItemPrice(long branchId, Guid itemId, [FromBody] SetIFoodItemPriceRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodItemPrice), async () =>
        {
            var result = await Mediator.Send(new SetIFoodItemPriceCommand(branchId, itemId, request.Value, request.OriginalValue, request.PriceByCatalog), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("ifood/catalog/branch/{branchId:long}/items/{itemId:guid}/externalCode")]
    public Task<IActionResult> SetIFoodItemExternalCode(long branchId, Guid itemId, [FromBody] SetIFoodItemExternalCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodItemExternalCode), async () =>
        {
            var result = await Mediator.Send(new SetIFoodItemExternalCodeCommand(branchId, itemId, request.ExternalCode, request.ByCatalog), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("ifood/catalog/branch/{branchId:long}/categories/{categoryId}/items/{productId:guid}")]
    public Task<IActionResult> DeleteIFoodItem(long branchId, string categoryId, Guid productId, [FromQuery] string? catalogContext, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIFoodItem), async () =>
        {
            var result = await Mediator.Send(new DeleteIFoodItemCommand(branchId, categoryId, productId, catalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/catalog/branch/{branchId:long}/categories/{categoryId}/items")]
    public Task<IActionResult> ListIFoodCategoryItems(long branchId, string categoryId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIFoodCategoryItems), async () =>
        {
            var result = await Mediator.Send(new ListIFoodCategoryItemsQuery(branchId, categoryId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // --- Option groups / options -----------------------------------------------------------------

    [HttpGet("ifood/catalog/branch/{branchId:long}/option-groups")]
    public Task<IActionResult> ListIFoodOptionGroups(long branchId, [FromQuery] bool includeOptions, [FromQuery] string? catalogContext, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIFoodOptionGroups), async () =>
        {
            var result = await Mediator.Send(new ListIFoodOptionGroupsQuery(branchId, includeOptions, catalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPatch("ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}")]
    public Task<IActionResult> UpdateIFoodOptionGroup(long branchId, Guid optionGroupId, [FromBody] UpdateIFoodOptionGroupRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(UpdateIFoodOptionGroup), async () =>
        {
            var result = await Mediator.Send(new UpdateIFoodOptionGroupCommand(branchId, optionGroupId, request.Name), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}")]
    public Task<IActionResult> DeleteIFoodOptionGroup(long branchId, Guid optionGroupId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIFoodOptionGroup), async () =>
        {
            var result = await Mediator.Send(new DeleteIFoodOptionGroupCommand(branchId, optionGroupId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}/products/{productId:guid}")]
    public Task<IActionResult> DisassociateIFoodOptionGroup(long branchId, Guid optionGroupId, Guid productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DisassociateIFoodOptionGroup), async () =>
        {
            var result = await Mediator.Send(new DisassociateIFoodOptionGroupCommand(branchId, optionGroupId, productId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}/options/{productId:guid}")]
    public Task<IActionResult> DeleteIFoodOption(long branchId, Guid optionGroupId, Guid productId, [FromQuery] string? catalogContext, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIFoodOption), async () =>
        {
            var result = await Mediator.Send(new DeleteIFoodOptionCommand(branchId, optionGroupId, productId, catalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPatch("ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}/status")]
    public Task<IActionResult> UpdateIFoodOptionGroupStatus(long branchId, Guid optionGroupId, [FromBody] UpdateIFoodOptionGroupStatusRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(UpdateIFoodOptionGroupStatus), async () =>
        {
            var result = await Mediator.Send(new UpdateIFoodOptionGroupStatusCommand(branchId, optionGroupId, request.Available), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("ifood/catalog/branch/{branchId:long}/options/{optionId:guid}/price")]
    public Task<IActionResult> SetIFoodOptionPrice(long branchId, Guid optionId, [FromBody] SetIFoodOptionPriceRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodOptionPrice), async () =>
        {
            var result = await Mediator.Send(new SetIFoodOptionPriceCommand(branchId, optionId, request.Value, request.OriginalValue, request.ParentCustomizationOptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("ifood/catalog/branch/{branchId:long}/options/{optionId:guid}/externalCode")]
    public Task<IActionResult> SetIFoodOptionExternalCode(long branchId, Guid optionId, [FromBody] SetIFoodOptionExternalCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodOptionExternalCode), async () =>
        {
            var result = await Mediator.Send(new SetIFoodOptionExternalCodeCommand(branchId, optionId, request.ExternalCode, request.ParentCustomizationOptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPatch("ifood/catalog/branch/{branchId:long}/options/{optionId:guid}/status")]
    public Task<IActionResult> SetIFoodOptionStatus(long branchId, Guid optionId, [FromBody] SetIFoodOptionStatusRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodOptionStatus), async () =>
        {
            var result = await Mediator.Send(new SetIFoodOptionStatusCommand(branchId, optionId, request.Available, request.ParentCustomizationOptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // --- Admin (estoque, lote, versão do catálogo, imagem) --------------------------------------

    [HttpGet("ifood/catalog/branch/{branchId:long}/inventory/{productId:guid}")]
    public Task<IActionResult> GetIFoodInventory(long branchId, Guid productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodInventory), async () =>
        {
            var result = await Mediator.Send(new GetIFoodInventoryQuery(branchId, productId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpDelete("ifood/catalog/branch/{branchId:long}/inventory/batch")]
    public Task<IActionResult> DeleteIFoodInventoryBatch(long branchId, [FromBody] DeleteIFoodInventoryBatchRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIFoodInventoryBatch), async () =>
        {
            var result = await Mediator.Send(new DeleteIFoodInventoryBatchCommand(branchId, request.ProductIds), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/catalog/branch/{branchId:long}/batch/{batchId}")]
    public Task<IActionResult> GetIFoodBatchResult(long branchId, string batchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodBatchResult), async () =>
        {
            var result = await Mediator.Send(new GetIFoodBatchResultQuery(branchId, batchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/catalog/branch/{branchId:long}/version")]
    public Task<IActionResult> CheckIFoodCatalogVersion(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CheckIFoodCatalogVersion), async () =>
        {
            var result = await Mediator.Send(new CheckIFoodCatalogVersionQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // ⚠️ Upgrade/downgrade são operações destrutivas e irreversíveis no catálogo real do merchant —
    // ver comentário completo em UpgradeIFoodCatalogVersionCommand/DowngradeIFoodCatalogVersionCommand.
    // A UI precisa confirmar explicitamente com o usuário antes de chamar estes dois endpoints.
    [HttpPost("ifood/catalog/branch/{branchId:long}/upgrade")]
    public Task<IActionResult> UpgradeIFoodCatalogVersion(long branchId, [FromBody] UpgradeIFoodCatalogVersionRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(UpgradeIFoodCatalogVersion), async () =>
        {
            var result = await Mediator.Send(new UpgradeIFoodCatalogVersionCommand(branchId, request.CleanMigration), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/catalog/branch/{branchId:long}/downgrade")]
    public Task<IActionResult> DowngradeIFoodCatalogVersion(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DowngradeIFoodCatalogVersion), async () =>
        {
            var result = await Mediator.Send(new DowngradeIFoodCatalogVersionCommand(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // ⚠️ Schema de corpo/resposta não documentado pelo iFood (Postman mostra "<object>" cru) — ver
    // ressalva completa em UploadIFoodImageCommand. Repassa o JSON cru fornecido pelo chamador.
    [HttpPost("ifood/catalog/branch/{branchId:long}/image")]
    public Task<IActionResult> UploadIFoodImage(long branchId, [FromBody] UploadIFoodImageRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(UploadIFoodImage), async () =>
        {
            var result = await Mediator.Send(new UploadIFoodImageCommand(branchId, request.JsonBody), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // --- v1 (legado) — console genérico ----------------------------------------------------------
    // Um único endpoint despachante pros 56 endpoints do Catalog v1 sem tipagem dedicada — ver
    // comentário completo em InvokeIFoodCatalogV1OperationCommand sobre a decisão de escopo.
    [HttpPost("ifood/catalog/branch/{branchId:long}/v1/invoke")]
    public Task<IActionResult> InvokeIFoodCatalogV1Operation(long branchId, [FromBody] InvokeIFoodCatalogV1OperationRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(InvokeIFoodCatalogV1Operation), async () =>
        {
            var result = await Mediator.Send(new InvokeIFoodCatalogV1OperationCommand(
                branchId, request.Operation, request.RouteParams, request.QueryParams, request.JsonBody), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Financeiro iFood (fase 4) — a sincronização em si roda sozinha em segundo plano 1x/dia
    // (IFoodFinancialSyncBackgroundService); estes endpoints são pra tela "Financeiro" ler o
    // resumo do período e pro botão "Sincronizar agora" reenviar sob demanda.
    [HttpGet("ifood/financial/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodFinancialSummary(long branchId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodFinancialSummary), async () =>
        {
            var result = await Mediator.Send(new GetIFoodFinancialSummaryQuery(branchId, from, to), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/financial/sync")]
    public Task<IActionResult> SyncIFoodFinancial([FromBody] SyncIFoodFinancialRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SyncIFoodFinancial), async () =>
        {
            var result = await Mediator.Send(new SyncIFoodFinancialCommand(request.CompanyId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Fase 9 — cobertura dos 13 relatórios financeiros restantes (financial/v2.0 ×12 +
    // financial/v2.1 ×1), mais anticipations/sales (financial/v3.0) via o mesmo catálogo
    // genérico. reportType é o nome do enum IFoodFinancialReportType (ex.: "SalesAdjustments").
    [HttpGet("ifood/financial/branch/{branchId:long}/reports/{reportType}")]
    public Task<IActionResult> GetIFoodFinancialReport(
        long branchId, IFoodFinancialReportType reportType, [FromQuery] string? periodId,
        [FromQuery] DateTime? rangeStart, [FromQuery] DateTime? rangeEnd, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodFinancialReport), async () =>
        {
            var result = await Mediator.Send(new GetIFoodFinancialReportQuery(branchId, reportType, periodId, rangeStart, rangeEnd), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Fase 9 — apuração sob demanda (financial/v3.0/.../reconciliation/on-demand), pra quando a
    // apuração automática do período ainda não foi gerada. Competence no formato "yyyy-MM".
    [HttpPost("ifood/financial/branch/{branchId:long}/reconciliation-on-demand")]
    public Task<IActionResult> RequestIFoodReconciliationOnDemand(
        long branchId, [FromBody] RequestIFoodReconciliationOnDemandRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodReconciliationOnDemand), async () =>
        {
            var result = await Mediator.Send(new RequestIFoodReconciliationOnDemandCommand(branchId, request.Competence), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/financial/branch/{branchId:long}/reconciliation-on-demand/{requestId}")]
    public Task<IActionResult> GetIFoodReconciliationOnDemandStatus(long branchId, string requestId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodReconciliationOnDemandStatus), async () =>
        {
            var result = await Mediator.Send(new GetIFoodReconciliationOnDemandStatusQuery(branchId, requestId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Operação da loja iFood (fase 5, módulo Merchant) — tudo sob demanda (sem sincronização
    // automática em segundo plano): status é lido ao vivo, interrupções são criadas/removidas
    // direto na API do iFood, horários e tempo de preparo são editados numa cópia local e
    // reenviados ao salvar (ver comentário nos handlers em Features/Integrations/IFood/Merchant).
    [HttpGet("ifood/merchant/status/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodMerchantStatus(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodMerchantStatus), async () =>
        {
            var result = await Mediator.Send(new GetIFoodMerchantStatusQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/merchant/interruptions/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodInterruptions(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodInterruptions), async () =>
        {
            var result = await Mediator.Send(new GetIFoodInterruptionsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/merchant/interruptions")]
    public Task<IActionResult> CreateIFoodInterruption([FromBody] CreateIFoodInterruptionCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CreateIFoodInterruption), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("ifood/merchant/interruptions/{interruptionId}")]
    public Task<IActionResult> DeleteIFoodInterruption(string interruptionId, [FromQuery] long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIFoodInterruption), async () =>
        {
            var result = await Mediator.Send(new DeleteIFoodInterruptionCommand(branchId, interruptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/merchant/opening-hours/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodOpeningHours(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOpeningHours), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOpeningHoursQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood/merchant/opening-hours")]
    public Task<IActionResult> SaveIFoodOpeningHours([FromBody] SaveIFoodOpeningHoursCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SaveIFoodOpeningHours), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("ifood/merchant/preparation-time")]
    public Task<IActionResult> SetIFoodPreparationTime([FromBody] SetIFoodPreparationTimeCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodPreparationTime), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Fase 9c — fecha os gaps restantes do módulo Merchant da auditoria de 2026-08-20: listar
    // lojas do client_id, ver detalhes de uma loja específica e consultar status por operação
    // (ex.: DELIVERY, TAKEOUT — diferente do status geral já coberto acima).
    [HttpGet("ifood/merchant/list/company/{companyId:long}")]
    public Task<IActionResult> GetIFoodMerchantsList(long companyId, [FromQuery] int page, [FromQuery] int size, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodMerchantsList), async () =>
        {
            var result = await Mediator.Send(new GetIFoodMerchantsListQuery(companyId, page <= 0 ? 1 : page, size <= 0 ? 100 : size), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/merchant/details/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodMerchantDetails(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodMerchantDetails), async () =>
        {
            var result = await Mediator.Send(new GetIFoodMerchantDetailsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/merchant/status/branch/{branchId:long}/operation/{operation}")]
    public Task<IActionResult> GetIFoodMerchantStatusByOperation(long branchId, string operation, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodMerchantStatusByOperation), async () =>
        {
            var result = await Mediator.Send(new GetIFoodMerchantStatusByOperationQuery(branchId, operation), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Logística por frota própria (fase 7, módulo Logistics) — só se aplica a pedidos DELIVERY
    // com deliveredBy diferente de "IFOOD" (ver IFoodOrder.DeliveredBy); tudo sob demanda, cada
    // passo é acionado manualmente pela equipe conforme o entregador avança (atribuir → saiu pra
    // origem → chegou na origem → despachou → chegou no destino → verificar código de entrega).
    [HttpGet("ifood/logistics/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodLogisticsDeliveries(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodLogisticsDeliveries), async () =>
        {
            var result = await Mediator.Send(new GetIFoodLogisticsDeliveriesQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/assign-driver")]
    public Task<IActionResult> AssignIFoodDriver(long ifoodOrderId, [FromBody] AssignIFoodDriverRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AssignIFoodDriver), async () =>
        {
            var result = await Mediator.Send(new AssignIFoodDriverCommand(ifoodOrderId, request.DriverName, request.DriverPhone, request.DriverVehicleType), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/going-to-origin")]
    public Task<IActionResult> MarkIFoodGoingToOrigin(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIFoodGoingToOrigin), async () =>
        {
            var result = await Mediator.Send(new MarkIFoodGoingToOriginCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/arrived-at-origin")]
    public Task<IActionResult> MarkIFoodArrivedAtOrigin(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIFoodArrivedAtOrigin), async () =>
        {
            var result = await Mediator.Send(new MarkIFoodArrivedAtOriginCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/dispatch")]
    public Task<IActionResult> DispatchIFoodLogistics(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DispatchIFoodLogistics), async () =>
        {
            var result = await Mediator.Send(new DispatchIFoodLogisticsCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/arrived-at-destination")]
    public Task<IActionResult> MarkIFoodArrivedAtDestination(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIFoodArrivedAtDestination), async () =>
        {
            var result = await Mediator.Send(new MarkIFoodArrivedAtDestinationCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/verify-delivery-code")]
    public Task<IActionResult> VerifyIFoodDeliveryCode(long ifoodOrderId, [FromBody] VerifyIFoodDeliveryCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(VerifyIFoodDeliveryCode), async () =>
        {
            var result = await Mediator.Send(new VerifyIFoodDeliveryCodeCommand(ifoodOrderId, request.Code), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { codeMatched = result.Value });
        });

    // Fase 9c — fecha o gap restante do módulo Logistics da auditoria de 2026-08-20: detalhes da
    // entrega direto no iFood (resposta sem schema documentado — ver IFoodLogisticsOrderDetailsResult).
    [HttpGet("ifood/logistics/order/{ifoodOrderId:long}/details")]
    public Task<IActionResult> GetIFoodLogisticsOrderDetails(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodLogisticsOrderDetails), async () =>
        {
            var result = await Mediator.Send(new GetIFoodLogisticsOrderDetailsQuery(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Shipping (fase 8, módulo Shipping) — entrega, via malha de entregadores do iFood, de
    // pedidos que NÃO vieram do iFood (telefone, WhatsApp, site próprio). Tudo sob demanda: a
    // equipe cota o endereço, confirma o pedido de motorista, acompanha o rastreamento e cancela
    // se preciso. Também cobre a variante "pedido já existente no iFood" (quote/requestDriver/
    // cancelRequestDriver sobre um ifoodOrderId), que fecha uma lacuna do módulo Order.
    [HttpGet("ifood/shipping/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodShippingDeliveries(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodShippingDeliveries), async () =>
        {
            var result = await Mediator.Send(new GetIFoodShippingDeliveriesQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/shipping/branch/{branchId:long}/quote")]
    public Task<IActionResult> GetIFoodShippingQuote(long branchId, [FromQuery] double latitude, [FromQuery] double longitude, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodShippingQuote), async () =>
        {
            var result = await Mediator.Send(new GetIFoodShippingQuoteQuery(branchId, latitude, longitude), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/shipping")]
    public Task<IActionResult> RequestIFoodShippingDriver([FromBody] RequestIFoodShippingDriverCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodShippingDriver), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { id = result.Value });
        });

    [HttpGet("ifood/shipping/{id:long}/tracking")]
    public Task<IActionResult> GetIFoodShippingTracking(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodShippingTracking), async () =>
        {
            var result = await Mediator.Send(new GetIFoodShippingTrackingQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/shipping/{id:long}/cancellation-reasons")]
    public Task<IActionResult> GetIFoodShippingCancellationReasons(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodShippingCancellationReasons), async () =>
        {
            var result = await Mediator.Send(new GetIFoodShippingCancellationReasonsQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/shipping/{id:long}/cancel")]
    public Task<IActionResult> CancelIFoodShippingDelivery(long id, [FromBody] CancelIFoodShippingDeliveryRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIFoodShippingDelivery), async () =>
        {
            var result = await Mediator.Send(new CancelIFoodShippingDeliveryCommand(id, request.Reason, request.CancellationCode), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/shipping/{id:long}/safe-delivery-score")]
    public Task<IActionResult> GetIFoodSafeDeliveryScore(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodSafeDeliveryScore), async () =>
        {
            var result = await Mediator.Send(new GetIFoodSafeDeliveryScoreQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Variante "pedido já existente no iFood" (mesmo módulo Shipping, atua sobre um ifoodOrderId).
    [HttpGet("ifood/shipping/order/{ifoodOrderId:long}/quote")]
    public Task<IActionResult> GetIFoodOrderShippingQuote(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOrderShippingQuote), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOrderShippingQuoteQuery(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/shipping/order/{ifoodOrderId:long}/request-driver")]
    public Task<IActionResult> RequestIFoodOrderShippingDriver(long ifoodOrderId, [FromBody] RequestIFoodOrderShippingDriverRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodOrderShippingDriver), async () =>
        {
            var result = await Mediator.Send(new RequestIFoodOrderShippingDriverCommand(ifoodOrderId, request.QuoteId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/shipping/order/{ifoodOrderId:long}/cancel-request-driver")]
    public Task<IActionResult> CancelIFoodOrderShippingDriver(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIFoodOrderShippingDriver), async () =>
        {
            var result = await Mediator.Send(new CancelIFoodOrderShippingDriverCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Fase 11 — fecha os últimos 4 endpoints da auditoria (troca de endereço de entrega em
    // andamento, mesma variante "pedido já existente no iFood" acima).
    [HttpPost("ifood/shipping/order/{ifoodOrderId:long}/delivery-address-change")]
    public Task<IActionResult> RequestIFoodDeliveryAddressChange(long ifoodOrderId, [FromBody] RequestIFoodDeliveryAddressChangeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodDeliveryAddressChange), async () =>
        {
            var result = await Mediator.Send(new RequestDeliveryAddressChangeCommand(
                ifoodOrderId, request.StreetNumber, request.StreetName, request.Complement, request.Neighborhood,
                request.City, request.State, request.Country, request.Reference, request.Latitude, request.Longitude), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/shipping/order/{ifoodOrderId:long}/delivery-address-change/accept")]
    public Task<IActionResult> AcceptIFoodDeliveryAddressChange(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AcceptIFoodDeliveryAddressChange), async () =>
        {
            var result = await Mediator.Send(new AcceptDeliveryAddressChangeCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/shipping/order/{ifoodOrderId:long}/delivery-address-change/deny")]
    public Task<IActionResult> DenyIFoodDeliveryAddressChange(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DenyIFoodDeliveryAddressChange), async () =>
        {
            var result = await Mediator.Send(new DenyDeliveryAddressChangeCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/shipping/order/{ifoodOrderId:long}/user-confirm-address")]
    public Task<IActionResult> ConfirmIFoodUserAddress(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ConfirmIFoodUserAddress), async () =>
        {
            var result = await Mediator.Send(new ConfirmUserAddressCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Avaliações (fase 9, módulo Review v1.0) — sem persistência local, sempre lido/escrito
    // direto no iFood (ver comentário em IIFoodReviewClient).
    [HttpGet("ifood/reviews/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodReviews(
        long branchId, [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, [FromQuery] string? sort, [FromQuery] string? sortBy, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodReviews), async () =>
        {
            var result = await Mediator.Send(new GetIFoodReviewsQuery(
                branchId, page <= 0 ? 1 : page, pageSize <= 0 ? 10 : pageSize, dateFrom, dateTo,
                string.IsNullOrWhiteSpace(sort) ? "DESC" : sort, string.IsNullOrWhiteSpace(sortBy) ? "CREATED_AT" : sortBy), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/reviews/branch/{branchId:long}/{reviewId}")]
    public Task<IActionResult> GetIFoodReviewById(long branchId, string reviewId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodReviewById), async () =>
        {
            var result = await Mediator.Send(new GetIFoodReviewByIdQuery(branchId, reviewId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/reviews/branch/{branchId:long}/{reviewId}/reply")]
    public Task<IActionResult> ReplyIFoodReview(long branchId, string reviewId, [FromBody] ReplyIFoodReviewRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ReplyIFoodReview), async () =>
        {
            var result = await Mediator.Send(new ReplyIFoodReviewCommand(branchId, reviewId, request.Text), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/reviews/branch/{branchId:long}/summary")]
    public Task<IActionResult> GetIFoodReviewsSummary(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodReviewsSummary), async () =>
        {
            var result = await Mediator.Send(new GetIFoodReviewsSummaryQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Indicadores (fase 9, módulo Analytics v1.0) — 1 endpoint (KPIs de pedidos); ver ressalva
    // sobre o payload padrão usado em IIFoodAnalyticsClient.
    [HttpGet("ifood/analytics/branch/{branchId:long}/order-kpis")]
    public Task<IActionResult> GetIFoodOrderKpis(
        long branchId, [FromQuery] DateTime? periodStart, [FromQuery] DateTime? periodEnd, [FromQuery] int page, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOrderKpis), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOrderKpisQuery(branchId, periodStart, periodEnd, page), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Alertas operacionais do iFood (fase 13) — hoje só populados pelo
    // IFoodMerchantStatusWatcherBackgroundService (loja indisponível/disponível), guardados em
    // memória (IIFoodOperationalAlertStore). GET traz os não reconhecidos da empresa pra tela
    // mostrar em um sino no topo; ACK remove da lista (idempotente — reconhecer de novo não é erro).
    [HttpGet("ifood/alerts/company/{companyId:long}")]
    public Task<IActionResult> GetIFoodOperationalAlerts(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOperationalAlerts), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOperationalAlertsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/alerts/ack")]
    public Task<IActionResult> AcknowledgeIFoodOperationalAlert([FromBody] AcknowledgeIFoodOperationalAlertCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AcknowledgeIFoodOperationalAlert), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

// Fase Sonar MEDIUM (2026-08-24): [property: JsonRequired] nos campos de tipo valor para
// evitar under-posting.
public sealed record SyncIFoodCatalogRequest([property: JsonRequired] long CompanyId);

public sealed record CancelIFoodOrderRequest(string ReasonCode);

public sealed record ValidateIFoodPickupCodeRequest(string Code);

public sealed record IFoodDisputeActionRequest([property: JsonRequired] long BranchId);

public sealed record RejectIFoodDisputeRequest([property: JsonRequired] long BranchId, string Reason);

public sealed record RequestIFoodDisputeAlternativeRequest(
    [property: JsonRequired] long BranchId, string AlternativeType, decimal? Amount, string? Currency);

public sealed record VerifyIFoodOrderDeliveryCodeRequest(string Code);

public sealed record SyncIFoodFinancialRequest([property: JsonRequired] long CompanyId);

public sealed record AssignIFoodDriverRequest(string DriverName, string DriverPhone, string DriverVehicleType);

public sealed record VerifyIFoodDeliveryCodeRequest(string Code);

public sealed record CancelIFoodShippingDeliveryRequest(string Reason, [property: JsonRequired] int CancellationCode);

public sealed record RequestIFoodOrderShippingDriverRequest(string QuoteId);

// Fase 11 — payload do request de troca de endereço de entrega (ver IFoodShippingDeliveryAddressChangePayload).
public sealed record RequestIFoodDeliveryAddressChangeRequest(
    string StreetNumber, string StreetName, string? Complement, string Neighborhood, string City,
    string State, string? Country, string? Reference, double? Latitude, double? Longitude);

public sealed record RequestIFoodReconciliationOnDemandRequest(string Competence);

// Fase 10 — Catalog (ver seção correspondente em IntegrationsController acima).

public sealed record CreateIFoodCategoryRequest(string Name);

public sealed record EditIFoodCategoryRequest(string? Name, string? ExternalCode, string? Status, int? Index);

public sealed record CreateIFoodProductRequest(
    string? Id, string Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, string? Image, IReadOnlyCollection<IFoodProductShiftInput>? Shifts);

public sealed record EditIFoodProductRequest(
    string Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, string? Image, IReadOnlyCollection<IFoodProductShiftInput>? Shifts);

public sealed record BatchUpdateIFoodProductStatusesRequest(IReadOnlyCollection<IFoodBatchProductStatusInput> Items, string? CatalogContext);

public sealed record BatchUpdateIFoodProductPricesRequest(IReadOnlyCollection<IFoodBatchProductPriceInput> Items, string? CatalogContext);

public sealed record SetIFoodItemPriceRequest(
    [property: JsonRequired] decimal Value, decimal? OriginalValue, IReadOnlyCollection<IFoodItemPriceByCatalogInput>? PriceByCatalog);

public sealed record SetIFoodItemExternalCodeRequest(string? ExternalCode, IReadOnlyCollection<IFoodItemExternalCodeByCatalogInput>? ByCatalog);

public sealed record UpdateIFoodOptionGroupRequest(string Name);

public sealed record UpdateIFoodOptionGroupStatusRequest([property: JsonRequired] bool Available);

public sealed record SetIFoodOptionPriceRequest(
    [property: JsonRequired] decimal Value, decimal? OriginalValue, string? ParentCustomizationOptionId);

public sealed record SetIFoodOptionExternalCodeRequest(string ExternalCode, string? ParentCustomizationOptionId);

public sealed record SetIFoodOptionStatusRequest([property: JsonRequired] bool Available, string? ParentCustomizationOptionId);

public sealed record DeleteIFoodInventoryBatchRequest(IReadOnlyCollection<Guid> ProductIds);

public sealed record UpgradeIFoodCatalogVersionRequest(bool? CleanMigration);

public sealed record UploadIFoodImageRequest(string JsonBody);

public sealed record InvokeIFoodCatalogV1OperationRequest(
    [property: JsonRequired] IFoodCatalogV1Operation Operation,
    Dictionary<string, string>? RouteParams, Dictionary<string, string>? QueryParams, string? JsonBody);

public sealed record ReplyIFoodReviewRequest(string Text);
