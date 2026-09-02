using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Analytics;
using SyncBar.Application.Features.Integrations.Ifood.Catalog;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.V1Legacy;
using SyncBar.Application.Features.Integrations.Ifood.Financial;
using SyncBar.Application.Features.Integrations.Ifood.Logistics;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Application.Features.Integrations.Ifood.Review;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Repositories;
namespace SyncBar.API.Controllers;


public sealed class IntegrationsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("Ifood/company/{companyId:long}")]
    public Task<IActionResult> GetIfoodSettings(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodSettings), async () =>
        {
            var result = await Mediator.Send(new GetIfoodSettingsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("Ifood")]
    public Task<IActionResult> SaveIfoodSettings([FromBody] SaveIfoodSettingsCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SaveIfoodSettings), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/test-connection")]
    public Task<IActionResult> TestIfoodConnection([FromBody] TestIfoodConnectionCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(TestIfoodConnection), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/merchants/company/{companyId:long}")]
    public Task<IActionResult> GetIfoodMerchantMappings(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodMerchantMappings), async () =>
        {
            var result = await Mediator.Send(new GetIfoodMerchantMappingsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("Ifood/merchants")]
    public Task<IActionResult> SetIfoodMerchantMapping([FromBody] SetIfoodMerchantMappingCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIfoodMerchantMapping), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/orders/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodOrders(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodOrders), async () =>
        {
            var result = await Mediator.Send(new GetIfoodOrdersQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/orders/{IfoodOrderId:long}/start-preparation")]
    public Task<IActionResult> StartIfoodOrderPreparation(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(StartIfoodOrderPreparation), async () =>
        {
            var result = await Mediator.Send(new StartIfoodOrderPreparationCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/orders/{IfoodOrderId:long}/ready")]
    public Task<IActionResult> MarkIfoodOrderReady(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIfoodOrderReady), async () =>
        {
            var result = await Mediator.Send(new MarkIfoodOrderReadyCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/orders/{IfoodOrderId:long}/cancellation-reasons")]
    public Task<IActionResult> GetIfoodCancellationReasons(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodCancellationReasons), async () =>
        {
            var result = await Mediator.Send(new GetIfoodCancellationReasonsQuery(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/orders/{IfoodOrderId:long}/cancel")]
    public Task<IActionResult> CancelIfoodOrder(long IfoodOrderId, [FromBody] CancelIfoodOrderRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIfoodOrder), async () =>
        {
            var result = await Mediator.Send(new CancelIfoodOrderCommand(IfoodOrderId, request.ReasonCode), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/orders/{IfoodOrderId:long}/tracking")]
    public Task<IActionResult> GetIfoodOrderTracking(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodOrderTracking), async () =>
        {
            var result = await Mediator.Send(new GetIfoodOrderTrackingQuery(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/orders/{IfoodOrderId:long}/validate-pickup-code")]
    public Task<IActionResult> ValidateIfoodPickupCode(long IfoodOrderId, [FromBody] ValidateIfoodPickupCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ValidateIfoodPickupCode), async () =>
        {
            var result = await Mediator.Send(new ValidateIfoodPickupCodeCommand(IfoodOrderId, request.Code), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { codeMatched = result.Value });
        });

    [HttpPost("Ifood/disputes/{disputeId}/accept")]
    public Task<IActionResult> AcceptIfoodDispute(string disputeId, [FromBody] IfoodDisputeActionRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AcceptIfoodDispute), async () =>
        {
            var result = await Mediator.Send(new AcceptIfoodDisputeCommand(request.BranchId, disputeId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/disputes/{disputeId}/reject")]
    public Task<IActionResult> RejectIfoodDispute(string disputeId, [FromBody] RejectIfoodDisputeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RejectIfoodDispute), async () =>
        {
            var result = await Mediator.Send(new RejectIfoodDisputeCommand(request.BranchId, disputeId, request.Reason), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/disputes/{disputeId}/alternatives/{alternativeId}")]
    public Task<IActionResult> RequestIfoodDisputeAlternative(
        string disputeId, string alternativeId, [FromBody] RequestIfoodDisputeAlternativeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIfoodDisputeAlternative), async () =>
        {
            var result = await Mediator.Send(new RequestIfoodDisputeAlternativeCommand(
                request.BranchId, disputeId, alternativeId, request.AlternativeType, request.Amount, request.Currency), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/orders/{IfoodOrderId:long}/virtual-bag")]
    public Task<IActionResult> GetIfoodOrderVirtualBag(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodOrderVirtualBag), async () =>
        {
            var result = await Mediator.Send(new GetIfoodOrderVirtualBagQuery(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/orders/{IfoodOrderId:long}/request-driver")]
    public Task<IActionResult> RequestIfoodOrderDriver(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIfoodOrderDriver), async () =>
        {
            var result = await Mediator.Send(new RequestIfoodOrderDriverCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/orders/{IfoodOrderId:long}/cancel-request-driver")]
    public Task<IActionResult> CancelIfoodOrderDriverRequest(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIfoodOrderDriverRequest), async () =>
        {
            var result = await Mediator.Send(new CancelIfoodOrderDriverRequestCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/orders/{IfoodOrderId:long}/verify-delivery-code")]
    public Task<IActionResult> VerifyIfoodOrderDeliveryCode(long IfoodOrderId, [FromBody] VerifyIfoodOrderDeliveryCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(VerifyIfoodOrderDeliveryCode), async () =>
        {
            var result = await Mediator.Send(new VerifyIfoodOrderDeliveryCodeCommand(IfoodOrderId, request.Code), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { codeMatched = result.Value });
        });

    [HttpPost("Ifood/catalog/sync")]
    public Task<IActionResult> SyncIfoodCatalog([FromBody] SyncIfoodCatalogRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SyncIfoodCatalog), async () =>
        {
            var result = await Mediator.Send(new SyncIfoodCatalogCommand(request.CompanyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/catalogs")]
    public Task<IActionResult> GetIfoodCatalogs(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodCatalogs), async () =>
        {
            var result = await Mediator.Send(new GetIfoodCatalogsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/catalogs/{catalogId}/categories")]
    public Task<IActionResult> ListIfoodCategories(long branchId, string catalogId, [FromQuery] bool includeItems, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIfoodCategories), async () =>
        {
            var result = await Mediator.Send(new ListIfoodCategoriesQuery(branchId, catalogId, includeItems), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/catalogs/{catalogId}/categories/{categoryId}")]
    public Task<IActionResult> GetIfoodCategory(long branchId, string catalogId, string categoryId, [FromQuery] bool includeItems, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodCategory), async () =>
        {
            var result = await Mediator.Send(new GetIfoodCategoryQuery(branchId, catalogId, categoryId, includeItems), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/catalog/branch/{branchId:long}/catalogs/{catalogId}/categories")]
    public Task<IActionResult> CreateIfoodCategory(long branchId, string catalogId, [FromBody] CreateIfoodCategoryRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CreateIfoodCategory), async () =>
        {
            var result = await Mediator.Send(new CreateIfoodCategoryCommand(branchId, catalogId, request.Name), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("Ifood/catalog/branch/{branchId:long}/catalogs/{catalogId}/categories/{categoryId}")]
    public Task<IActionResult> EditIfoodCategory(long branchId, string catalogId, string categoryId, [FromBody] EditIfoodCategoryRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(EditIfoodCategory), async () =>
        {
            var result = await Mediator.Send(new EditIfoodCategoryCommand(branchId, catalogId, categoryId, request.Name, request.ExternalCode, request.Status, request.Index), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpDelete("Ifood/catalog/branch/{branchId:long}/categories/{categoryId}")]
    public Task<IActionResult> DeleteIfoodCategory(long branchId, string categoryId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIfoodCategory), async () =>
        {
            var result = await Mediator.Send(new DeleteIfoodCategoryCommand(branchId, categoryId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/sellable-items")]
    public Task<IActionResult> ListIfoodSellableItems(long branchId, [FromQuery] string groupId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIfoodSellableItems), async () =>
        {
            var result = await Mediator.Send(new ListIfoodSellableItemsQuery(branchId, groupId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/products")]
    public Task<IActionResult> ListIfoodProducts(long branchId, [FromQuery] int? limit, [FromQuery] int? page, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIfoodProducts), async () =>
        {
            var result = await Mediator.Send(new ListIfoodProductsQuery(branchId, limit, page), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/catalog/branch/{branchId:long}/products")]
    public Task<IActionResult> CreateIfoodProduct(long branchId, [FromBody] CreateIfoodProductRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CreateIfoodProduct), async () =>
        {
            var result = await Mediator.Send(new CreateIfoodProductCommand(
                branchId, request.Id, request.Name, request.Description, request.AdditionalInformation,
                request.ExternalCode, request.Ean, request.Image, request.Shifts), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("Ifood/catalog/branch/{branchId:long}/products/{productId:guid}")]
    public Task<IActionResult> EditIfoodProduct(long branchId, Guid productId, [FromBody] EditIfoodProductRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(EditIfoodProduct), async () =>
        {
            var result = await Mediator.Send(new EditIfoodProductCommand(
                branchId, productId, request.Name, request.Description, request.AdditionalInformation,
                request.ExternalCode, request.Ean, request.Image, request.Shifts), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpDelete("Ifood/catalog/branch/{branchId:long}/products/{productId:guid}")]
    public Task<IActionResult> DeleteIfoodProduct(long branchId, Guid productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIfoodProduct), async () =>
        {
            var result = await Mediator.Send(new DeleteIfoodProductCommand(branchId, productId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPatch("Ifood/catalog/branch/{branchId:long}/products/status")]
    public Task<IActionResult> BatchUpdateIfoodProductStatuses(long branchId, [FromBody] BatchUpdateIfoodProductStatusesRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(BatchUpdateIfoodProductStatuses), async () =>
        {
            var result = await Mediator.Send(new BatchUpdateIfoodProductStatusesCommand(branchId, request.Items, request.CatalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/catalog/branch/{branchId:long}/products/price")]
    public Task<IActionResult> BatchUpdateIfoodProductPrices(long branchId, [FromBody] BatchUpdateIfoodProductPricesRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(BatchUpdateIfoodProductPrices), async () =>
        {
            var result = await Mediator.Send(new BatchUpdateIfoodProductPricesCommand(branchId, request.Items, request.CatalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/products/externalCode/{externalCode}")]
    public Task<IActionResult> ListIfoodProductsByExternalCode(long branchId, string externalCode, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIfoodProductsByExternalCode), async () =>
        {
            var result = await Mediator.Send(new ListIfoodProductsByExternalCodeQuery(branchId, externalCode), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/products/{productId:guid}")]
    public Task<IActionResult> GetIfoodProductById(long branchId, Guid productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodProductById), async () =>
        {
            var result = await Mediator.Send(new GetIfoodProductByIdQuery(branchId, productId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/items/{itemId:guid}")]
    public Task<IActionResult> GetIfoodItemFlat(long branchId, Guid itemId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodItemFlat), async () =>
        {
            var result = await Mediator.Send(new GetIfoodItemFlatQuery(branchId, itemId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("Ifood/catalog/branch/{branchId:long}/items/{itemId:guid}/price")]
    public Task<IActionResult> SetIfoodItemPrice(long branchId, Guid itemId, [FromBody] SetIfoodItemPriceRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIfoodItemPrice), async () =>
        {
            var result = await Mediator.Send(new SetIfoodItemPriceCommand(branchId, itemId, request.Value, request.OriginalValue, request.PriceByCatalog), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("Ifood/catalog/branch/{branchId:long}/items/{itemId:guid}/externalCode")]
    public Task<IActionResult> SetIfoodItemExternalCode(long branchId, Guid itemId, [FromBody] SetIfoodItemExternalCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIfoodItemExternalCode), async () =>
        {
            var result = await Mediator.Send(new SetIfoodItemExternalCodeCommand(branchId, itemId, request.ExternalCode, request.ByCatalog), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("Ifood/catalog/branch/{branchId:long}/categories/{categoryId}/items/{productId:guid}")]
    public Task<IActionResult> DeleteIfoodItem(long branchId, string categoryId, Guid productId, [FromQuery] string? catalogContext, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIfoodItem), async () =>
        {
            var result = await Mediator.Send(new DeleteIfoodItemCommand(branchId, categoryId, productId, catalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/categories/{categoryId}/items")]
    public Task<IActionResult> ListIfoodCategoryItems(long branchId, string categoryId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIfoodCategoryItems), async () =>
        {
            var result = await Mediator.Send(new ListIfoodCategoryItemsQuery(branchId, categoryId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/option-groups")]
    public Task<IActionResult> ListIfoodOptionGroups(long branchId, [FromQuery] bool includeOptions, [FromQuery] string? catalogContext, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ListIfoodOptionGroups), async () =>
        {
            var result = await Mediator.Send(new ListIfoodOptionGroupsQuery(branchId, includeOptions, catalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPatch("Ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}")]
    public Task<IActionResult> UpdateIfoodOptionGroup(long branchId, Guid optionGroupId, [FromBody] UpdateIfoodOptionGroupRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(UpdateIfoodOptionGroup), async () =>
        {
            var result = await Mediator.Send(new UpdateIfoodOptionGroupCommand(branchId, optionGroupId, request.Name), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("Ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}")]
    public Task<IActionResult> DeleteIfoodOptionGroup(long branchId, Guid optionGroupId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIfoodOptionGroup), async () =>
        {
            var result = await Mediator.Send(new DeleteIfoodOptionGroupCommand(branchId, optionGroupId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("Ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}/products/{productId:guid}")]
    public Task<IActionResult> DisassociateIfoodOptionGroup(long branchId, Guid optionGroupId, Guid productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DisassociateIfoodOptionGroup), async () =>
        {
            var result = await Mediator.Send(new DisassociateIfoodOptionGroupCommand(branchId, optionGroupId, productId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("Ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}/options/{productId:guid}")]
    public Task<IActionResult> DeleteIfoodOption(long branchId, Guid optionGroupId, Guid productId, [FromQuery] string? catalogContext, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIfoodOption), async () =>
        {
            var result = await Mediator.Send(new DeleteIfoodOptionCommand(branchId, optionGroupId, productId, catalogContext), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPatch("Ifood/catalog/branch/{branchId:long}/option-groups/{optionGroupId:guid}/status")]
    public Task<IActionResult> UpdateIfoodOptionGroupStatus(long branchId, Guid optionGroupId, [FromBody] UpdateIfoodOptionGroupStatusRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(UpdateIfoodOptionGroupStatus), async () =>
        {
            var result = await Mediator.Send(new UpdateIfoodOptionGroupStatusCommand(branchId, optionGroupId, request.Available), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("Ifood/catalog/branch/{branchId:long}/options/{optionId:guid}/price")]
    public Task<IActionResult> SetIfoodOptionPrice(long branchId, Guid optionId, [FromBody] SetIfoodOptionPriceRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIfoodOptionPrice), async () =>
        {
            var result = await Mediator.Send(new SetIfoodOptionPriceCommand(branchId, optionId, request.Value, request.OriginalValue, request.ParentCustomizationOptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("Ifood/catalog/branch/{branchId:long}/options/{optionId:guid}/externalCode")]
    public Task<IActionResult> SetIfoodOptionExternalCode(long branchId, Guid optionId, [FromBody] SetIfoodOptionExternalCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIfoodOptionExternalCode), async () =>
        {
            var result = await Mediator.Send(new SetIfoodOptionExternalCodeCommand(branchId, optionId, request.ExternalCode, request.ParentCustomizationOptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPatch("Ifood/catalog/branch/{branchId:long}/options/{optionId:guid}/status")]
    public Task<IActionResult> SetIfoodOptionStatus(long branchId, Guid optionId, [FromBody] SetIfoodOptionStatusRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIfoodOptionStatus), async () =>
        {
            var result = await Mediator.Send(new SetIfoodOptionStatusCommand(branchId, optionId, request.Available, request.ParentCustomizationOptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/inventory/{productId:guid}")]
    public Task<IActionResult> GetIfoodInventory(long branchId, Guid productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodInventory), async () =>
        {
            var result = await Mediator.Send(new GetIfoodInventoryQuery(branchId, productId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpDelete("Ifood/catalog/branch/{branchId:long}/inventory/batch")]
    public Task<IActionResult> DeleteIfoodInventoryBatch(long branchId, [FromBody] DeleteIfoodInventoryBatchRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIfoodInventoryBatch), async () =>
        {
            var result = await Mediator.Send(new DeleteIfoodInventoryBatchCommand(branchId, request.ProductIds), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/batch/{batchId}")]
    public Task<IActionResult> GetIfoodBatchResult(long branchId, string batchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodBatchResult), async () =>
        {
            var result = await Mediator.Send(new GetIfoodBatchResultQuery(branchId, batchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/catalog/branch/{branchId:long}/version")]
    public Task<IActionResult> CheckIfoodCatalogVersion(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CheckIfoodCatalogVersion), async () =>
        {
            var result = await Mediator.Send(new CheckIfoodCatalogVersionQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/catalog/branch/{branchId:long}/upgrade")]
    public Task<IActionResult> UpgradeIfoodCatalogVersion(long branchId, [FromBody] UpgradeIfoodCatalogVersionRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(UpgradeIfoodCatalogVersion), async () =>
        {
            var result = await Mediator.Send(new UpgradeIfoodCatalogVersionCommand(branchId, request.CleanMigration), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/catalog/branch/{branchId:long}/downgrade")]
    public Task<IActionResult> DowngradeIfoodCatalogVersion(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DowngradeIfoodCatalogVersion), async () =>
        {
            var result = await Mediator.Send(new DowngradeIfoodCatalogVersionCommand(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/catalog/branch/{branchId:long}/image")]
    public Task<IActionResult> UploadIfoodImage(long branchId, [FromBody] UploadIfoodImageRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(UploadIfoodImage), async () =>
        {
            var result = await Mediator.Send(new UploadIfoodImageCommand(branchId, request.JsonBody), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/catalog/branch/{branchId:long}/v1/invoke")]
    public Task<IActionResult> InvokeIfoodCatalogV1Operation(long branchId, [FromBody] InvokeIfoodCatalogV1OperationRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(InvokeIfoodCatalogV1Operation), async () =>
        {
            var result = await Mediator.Send(new InvokeIfoodCatalogV1OperationCommand(
                branchId, request.Operation, request.RouteParams, request.QueryParams, request.JsonBody), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/financial/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodFinancialSummary(long branchId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodFinancialSummary), async () =>
        {
            var result = await Mediator.Send(new GetIfoodFinancialSummaryQuery(branchId, from, to), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/financial/sync")]
    public Task<IActionResult> SyncIfoodFinancial([FromBody] SyncIfoodFinancialRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SyncIfoodFinancial), async () =>
        {
            var result = await Mediator.Send(new SyncIfoodFinancialCommand(request.CompanyId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/financial/branch/{branchId:long}/reports/{reportType}")]
    public Task<IActionResult> GetIfoodFinancialReport(
        long branchId, IfoodFinancialReportType reportType, [FromQuery] string? periodId,
        [FromQuery] DateTime? rangeStart, [FromQuery] DateTime? rangeEnd, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodFinancialReport), async () =>
        {
            var result = await Mediator.Send(new GetIfoodFinancialReportQuery(branchId, reportType, periodId, rangeStart, rangeEnd), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/financial/branch/{branchId:long}/reconciliation-on-demand")]
    public Task<IActionResult> RequestIfoodReconciliationOnDemand(
        long branchId, [FromBody] RequestIfoodReconciliationOnDemandRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIfoodReconciliationOnDemand), async () =>
        {
            var result = await Mediator.Send(new RequestIfoodReconciliationOnDemandCommand(branchId, request.Competence), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/financial/branch/{branchId:long}/reconciliation-on-demand/{requestId}")]
    public Task<IActionResult> GetIfoodReconciliationOnDemandStatus(long branchId, string requestId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodReconciliationOnDemandStatus), async () =>
        {
            var result = await Mediator.Send(new GetIfoodReconciliationOnDemandStatusQuery(branchId, requestId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/merchant/status/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodMerchantStatus(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodMerchantStatus), async () =>
        {
            var result = await Mediator.Send(new GetIfoodMerchantStatusQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/merchant/interruptions/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodInterruptions(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodInterruptions), async () =>
        {
            var result = await Mediator.Send(new GetIfoodInterruptionsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/merchant/interruptions")]
    public Task<IActionResult> CreateIfoodInterruption([FromBody] CreateIfoodInterruptionCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CreateIfoodInterruption), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("Ifood/merchant/interruptions/{interruptionId}")]
    public Task<IActionResult> DeleteIfoodInterruption(string interruptionId, [FromQuery] long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIfoodInterruption), async () =>
        {
            var result = await Mediator.Send(new DeleteIfoodInterruptionCommand(branchId, interruptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/merchant/opening-hours/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodOpeningHours(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodOpeningHours), async () =>
        {
            var result = await Mediator.Send(new GetIfoodOpeningHoursQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("Ifood/merchant/opening-hours")]
    public Task<IActionResult> SaveIfoodOpeningHours([FromBody] SaveIfoodOpeningHoursCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SaveIfoodOpeningHours), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("Ifood/merchant/preparation-time")]
    public Task<IActionResult> SetIfoodPreparationTime([FromBody] SetIfoodPreparationTimeCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIfoodPreparationTime), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/merchant/list/company/{companyId:long}")]
    public Task<IActionResult> GetIfoodMerchantsList(long companyId, [FromQuery] int page, [FromQuery] int size, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodMerchantsList), async () =>
        {
            var result = await Mediator.Send(new GetIfoodMerchantsListQuery(companyId, page <= 0 ? 1 : page, size <= 0 ? 100 : size), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/merchant/details/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodMerchantDetails(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodMerchantDetails), async () =>
        {
            var result = await Mediator.Send(new GetIfoodMerchantDetailsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/merchant/status/branch/{branchId:long}/operation/{operation}")]
    public Task<IActionResult> GetIfoodMerchantStatusByOperation(long branchId, string operation, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodMerchantStatusByOperation), async () =>
        {
            var result = await Mediator.Send(new GetIfoodMerchantStatusByOperationQuery(branchId, operation), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/logistics/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodLogisticsDeliveries(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodLogisticsDeliveries), async () =>
        {
            var result = await Mediator.Send(new GetIfoodLogisticsDeliveriesQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/logistics/order/{IfoodOrderId:long}/assign-driver")]
    public Task<IActionResult> AssignIfoodDriver(long IfoodOrderId, [FromBody] AssignIfoodDriverRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AssignIfoodDriver), async () =>
        {
            var result = await Mediator.Send(new AssignIfoodDriverCommand(IfoodOrderId, request.DriverName, request.DriverPhone, request.DriverVehicleType), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/logistics/order/{IfoodOrderId:long}/going-to-origin")]
    public Task<IActionResult> MarkIfoodGoingToOrigin(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIfoodGoingToOrigin), async () =>
        {
            var result = await Mediator.Send(new MarkIfoodGoingToOriginCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/logistics/order/{IfoodOrderId:long}/arrived-at-origin")]
    public Task<IActionResult> MarkIfoodArrivedAtOrigin(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIfoodArrivedAtOrigin), async () =>
        {
            var result = await Mediator.Send(new MarkIfoodArrivedAtOriginCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/logistics/order/{IfoodOrderId:long}/dispatch")]
    public Task<IActionResult> DispatchIfoodLogistics(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DispatchIfoodLogistics), async () =>
        {
            var result = await Mediator.Send(new DispatchIfoodLogisticsCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/logistics/order/{IfoodOrderId:long}/arrived-at-destination")]
    public Task<IActionResult> MarkIfoodArrivedAtDestination(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIfoodArrivedAtDestination), async () =>
        {
            var result = await Mediator.Send(new MarkIfoodArrivedAtDestinationCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/logistics/order/{IfoodOrderId:long}/verify-delivery-code")]
    public Task<IActionResult> VerifyIfoodDeliveryCode(long IfoodOrderId, [FromBody] VerifyIfoodDeliveryCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(VerifyIfoodDeliveryCode), async () =>
        {
            var result = await Mediator.Send(new VerifyIfoodDeliveryCodeCommand(IfoodOrderId, request.Code), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { codeMatched = result.Value });
        });

    [HttpGet("Ifood/logistics/order/{IfoodOrderId:long}/details")]
    public Task<IActionResult> GetIfoodLogisticsOrderDetails(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodLogisticsOrderDetails), async () =>
        {
            var result = await Mediator.Send(new GetIfoodLogisticsOrderDetailsQuery(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/shipping/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodShippingDeliveries(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodShippingDeliveries), async () =>
        {
            var result = await Mediator.Send(new GetIfoodShippingDeliveriesQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/shipping/branch/{branchId:long}/quote")]
    public Task<IActionResult> GetIfoodShippingQuote(long branchId, [FromQuery] double latitude, [FromQuery] double longitude, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodShippingQuote), async () =>
        {
            var result = await Mediator.Send(new GetIfoodShippingQuoteQuery(branchId, latitude, longitude), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/shipping")]
    public Task<IActionResult> RequestIfoodShippingDriver([FromBody] RequestIfoodShippingDriverCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIfoodShippingDriver), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { id = result.Value });
        });

    [HttpGet("Ifood/shipping/{id:long}/tracking")]
    public Task<IActionResult> GetIfoodShippingTracking(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodShippingTracking), async () =>
        {
            var result = await Mediator.Send(new GetIfoodShippingTrackingQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/shipping/{id:long}/cancellation-reasons")]
    public Task<IActionResult> GetIfoodShippingCancellationReasons(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodShippingCancellationReasons), async () =>
        {
            var result = await Mediator.Send(new GetIfoodShippingCancellationReasonsQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/shipping/{id:long}/cancel")]
    public Task<IActionResult> CancelIfoodShippingDelivery(long id, [FromBody] CancelIfoodShippingDeliveryRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIfoodShippingDelivery), async () =>
        {
            var result = await Mediator.Send(new CancelIfoodShippingDeliveryCommand(id, request.Reason, request.CancellationCode), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/shipping/{id:long}/safe-delivery-score")]
    public Task<IActionResult> GetIfoodSafeDeliveryScore(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodSafeDeliveryScore), async () =>
        {
            var result = await Mediator.Send(new GetIfoodSafeDeliveryScoreQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/shipping/order/{IfoodOrderId:long}/quote")]
    public Task<IActionResult> GetIfoodOrderShippingQuote(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodOrderShippingQuote), async () =>
        {
            var result = await Mediator.Send(new GetIfoodOrderShippingQuoteQuery(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/shipping/order/{IfoodOrderId:long}/request-driver")]
    public Task<IActionResult> RequestIfoodOrderShippingDriver(long IfoodOrderId, [FromBody] RequestIfoodOrderShippingDriverRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIfoodOrderShippingDriver), async () =>
        {
            var result = await Mediator.Send(new RequestIfoodOrderShippingDriverCommand(IfoodOrderId, request.QuoteId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/shipping/order/{IfoodOrderId:long}/cancel-request-driver")]
    public Task<IActionResult> CancelIfoodOrderShippingDriver(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIfoodOrderShippingDriver), async () =>
        {
            var result = await Mediator.Send(new CancelIfoodOrderShippingDriverCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/shipping/order/{IfoodOrderId:long}/delivery-address-change")]
    public Task<IActionResult> RequestIfoodDeliveryAddressChange(long IfoodOrderId, [FromBody] RequestIfoodDeliveryAddressChangeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIfoodDeliveryAddressChange), async () =>
        {
            var result = await Mediator.Send(new RequestDeliveryAddressChangeCommand(
                IfoodOrderId, request.StreetNumber, request.StreetName, request.Complement, request.Neighborhood,
                request.City, request.State, request.Country, request.Reference, request.Latitude, request.Longitude), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/shipping/order/{IfoodOrderId:long}/delivery-address-change/accept")]
    public Task<IActionResult> AcceptIfoodDeliveryAddressChange(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AcceptIfoodDeliveryAddressChange), async () =>
        {
            var result = await Mediator.Send(new AcceptDeliveryAddressChangeCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/shipping/order/{IfoodOrderId:long}/delivery-address-change/deny")]
    public Task<IActionResult> DenyIfoodDeliveryAddressChange(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DenyIfoodDeliveryAddressChange), async () =>
        {
            var result = await Mediator.Send(new DenyDeliveryAddressChangeCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("Ifood/shipping/order/{IfoodOrderId:long}/user-confirm-address")]
    public Task<IActionResult> ConfirmIfoodUserAddress(long IfoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ConfirmIfoodUserAddress), async () =>
        {
            var result = await Mediator.Send(new ConfirmUserAddressCommand(IfoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("Ifood/reviews/branch/{branchId:long}")]
    public Task<IActionResult> GetIfoodReviews(
        long branchId, [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, [FromQuery] string? sort, [FromQuery] string? sortBy, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodReviews), async () =>
        {
            var result = await Mediator.Send(new GetIfoodReviewsQuery(
                branchId, page <= 0 ? 1 : page, pageSize <= 0 ? 10 : pageSize, dateFrom, dateTo,
                string.IsNullOrWhiteSpace(sort) ? "DESC" : sort, string.IsNullOrWhiteSpace(sortBy) ? "CREATED_AT" : sortBy), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/reviews/branch/{branchId:long}/{reviewId}")]
    public Task<IActionResult> GetIfoodReviewById(long branchId, string reviewId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodReviewById), async () =>
        {
            var result = await Mediator.Send(new GetIfoodReviewByIdQuery(branchId, reviewId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/reviews/branch/{branchId:long}/{reviewId}/reply")]
    public Task<IActionResult> ReplyIfoodReview(long branchId, string reviewId, [FromBody] ReplyIfoodReviewRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ReplyIfoodReview), async () =>
        {
            var result = await Mediator.Send(new ReplyIfoodReviewCommand(branchId, reviewId, request.Text), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/reviews/branch/{branchId:long}/summary")]
    public Task<IActionResult> GetIfoodReviewsSummary(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodReviewsSummary), async () =>
        {
            var result = await Mediator.Send(new GetIfoodReviewsSummaryQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/analytics/branch/{branchId:long}/order-kpis")]
    public Task<IActionResult> GetIfoodOrderKpis(
        long branchId, [FromQuery] DateTime? periodStart, [FromQuery] DateTime? periodEnd, [FromQuery] int page, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodOrderKpis), async () =>
        {
            var result = await Mediator.Send(new GetIfoodOrderKpisQuery(branchId, periodStart, periodEnd, page), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("Ifood/alerts/company/{companyId:long}")]
    public Task<IActionResult> GetIfoodOperationalAlerts(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIfoodOperationalAlerts), async () =>
        {
            var result = await Mediator.Send(new GetIfoodOperationalAlertsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("Ifood/alerts/ack")]
    public Task<IActionResult> AcknowledgeIfoodOperationalAlert([FromBody] AcknowledgeIfoodOperationalAlertCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AcknowledgeIfoodOperationalAlert), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}
public sealed record SyncIfoodCatalogRequest([property: JsonRequired] long CompanyId);
public sealed record CancelIfoodOrderRequest(string ReasonCode);
public sealed record ValidateIfoodPickupCodeRequest(string Code);
public sealed record IfoodDisputeActionRequest([property: JsonRequired] long BranchId);
public sealed record RejectIfoodDisputeRequest([property: JsonRequired] long BranchId, string Reason);
public sealed record RequestIfoodDisputeAlternativeRequest(
    [property: JsonRequired] long BranchId, string AlternativeType, decimal? Amount, string? Currency);

public sealed record VerifyIfoodOrderDeliveryCodeRequest(string Code);

public sealed record SyncIfoodFinancialRequest([property: JsonRequired] long CompanyId);

public sealed record AssignIfoodDriverRequest(string DriverName, string DriverPhone, string DriverVehicleType);

public sealed record VerifyIfoodDeliveryCodeRequest(string Code);

public sealed record CancelIfoodShippingDeliveryRequest(string Reason, [property: JsonRequired] int CancellationCode);

public sealed record RequestIfoodOrderShippingDriverRequest(string QuoteId);

// Fase 11 — payload do request de troca de endereço de entrega (ver IfoodShippingDeliveryAddressChangePayload).
public sealed record RequestIfoodDeliveryAddressChangeRequest(
    string StreetNumber, string StreetName, string? Complement, string Neighborhood, string City,
    string State, string? Country, string? Reference, double? Latitude, double? Longitude);

public sealed record RequestIfoodReconciliationOnDemandRequest(string Competence);

// Fase 10 — Catalog (ver seção correspondente em IntegrationsController acima).

public sealed record CreateIfoodCategoryRequest(string Name);

public sealed record EditIfoodCategoryRequest(string? Name, string? ExternalCode, string? Status, int? Index);

public sealed record CreateIfoodProductRequest(
    string? Id, string Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, string? Image, IReadOnlyCollection<IfoodProductShiftInput>? Shifts);

public sealed record EditIfoodProductRequest(
    string Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, string? Image, IReadOnlyCollection<IfoodProductShiftInput>? Shifts);

public sealed record BatchUpdateIfoodProductStatusesRequest(IReadOnlyCollection<IfoodBatchProductStatusInput> Items, string? CatalogContext);

public sealed record BatchUpdateIfoodProductPricesRequest(IReadOnlyCollection<IfoodBatchProductPriceInput> Items, string? CatalogContext);

public sealed record SetIfoodItemPriceRequest(
    [property: JsonRequired] decimal Value, decimal? OriginalValue, IReadOnlyCollection<IfoodItemPriceByCatalogInput>? PriceByCatalog);

public sealed record SetIfoodItemExternalCodeRequest(string? ExternalCode, IReadOnlyCollection<IfoodItemExternalCodeByCatalogInput>? ByCatalog);

public sealed record UpdateIfoodOptionGroupRequest(string Name);

public sealed record UpdateIfoodOptionGroupStatusRequest([property: JsonRequired] bool Available);

public sealed record SetIfoodOptionPriceRequest(
    [property: JsonRequired] decimal Value, decimal? OriginalValue, string? ParentCustomizationOptionId);

public sealed record SetIfoodOptionExternalCodeRequest(string ExternalCode, string? ParentCustomizationOptionId);

public sealed record SetIfoodOptionStatusRequest([property: JsonRequired] bool Available, string? ParentCustomizationOptionId);

public sealed record DeleteIfoodInventoryBatchRequest(IReadOnlyCollection<Guid> ProductIds);

public sealed record UpgradeIfoodCatalogVersionRequest(bool? CleanMigration);

public sealed record UploadIfoodImageRequest(string JsonBody);

public sealed record InvokeIfoodCatalogV1OperationRequest(
    [property: JsonRequired] IfoodCatalogV1Operation Operation,
    Dictionary<string, string>? RouteParams, Dictionary<string, string>? QueryParams, string? JsonBody);

public sealed record ReplyIfoodReviewRequest(string Text);
