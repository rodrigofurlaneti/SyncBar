using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
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
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class IntegrationsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IntegrationsController _controller;

    public IntegrationsControllerTests()
    {
        _controller = new IntegrationsController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    // ---- Settings ----

    [Fact]
    public async Task GetIfoodSettings_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodSettingsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodSettingsResponse)null!));

        var result = await _controller.GetIfoodSettings(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodSettings_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodSettingsResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodSettings(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SaveIfoodSettings_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new SaveIfoodSettingsCommand(1, "client-id", "client-secret", true, "cust-1");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SaveIfoodSettings(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SaveIfoodSettings_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SaveIfoodSettingsCommand(999, null, null, false);
        _mediator.Send(Arg.Any<SaveIfoodSettingsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SaveIfoodSettings(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task TestIfoodConnection_Success_ShouldSendCommandAndReturnOk()
    {
        var command = new TestIfoodConnectionCommand(1);
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TestIfoodConnectionResponse(true, null)));

        var result = await _controller.TestIfoodConnection(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TestIfoodConnection_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new TestIfoodConnectionCommand(999);
        _mediator.Send(Arg.Any<TestIfoodConnectionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TestIfoodConnectionResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.TestIfoodConnection(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Merchant mappings ----

    [Fact]
    public async Task GetIfoodMerchantMappings_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodMerchantMappingsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodMerchantMappingResponse>>([]));

        var result = await _controller.GetIfoodMerchantMappings(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodMerchantMappings_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodMerchantMappingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodMerchantMappingResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodMerchantMappings(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetIfoodMerchantMapping_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new SetIfoodMerchantMappingCommand(1, "merchant-1", "uuid-1");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetIfoodMerchantMapping(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetIfoodMerchantMapping_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SetIfoodMerchantMappingCommand(999, null, null);
        _mediator.Send(Arg.Any<SetIfoodMerchantMappingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SetIfoodMerchantMapping(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Orders ----

    [Fact]
    public async Task GetIfoodOrders_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodOrdersQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodOrderResponse>>([]));

        var result = await _controller.GetIfoodOrders(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOrders_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodOrdersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodOrderResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodOrders(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task StartIfoodOrderPreparation_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new StartIfoodOrderPreparationCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.StartIfoodOrderPreparation(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task StartIfoodOrderPreparation_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<StartIfoodOrderPreparationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.StartIfoodOrderPreparation(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkIfoodOrderReady_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new MarkIfoodOrderReadyCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.MarkIfoodOrderReady(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkIfoodOrderReady_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<MarkIfoodOrderReadyCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.MarkIfoodOrderReady(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodCancellationReasons_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodCancellationReasonsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodCancellationReasonResponse>>([]));

        var result = await _controller.GetIfoodCancellationReasons(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodCancellationReasons_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodCancellationReasonsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodCancellationReasonResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodCancellationReasons(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelIfoodOrder_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new CancelIfoodOrderCommand(1, "CANCEL_REASON"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.CancelIfoodOrder(1, new CancelIfoodOrderRequest("CANCEL_REASON"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelIfoodOrder_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CancelIfoodOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.CancelIfoodOrder(999, new CancelIfoodOrderRequest("CANCEL_REASON"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOrderTracking_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodOrderTrackingQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodOrderTrackingResponse)null!));

        var result = await _controller.GetIfoodOrderTracking(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOrderTracking_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodOrderTrackingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodOrderTrackingResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodOrderTracking(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ValidateIfoodPickupCode_Success_ShouldSendCommandAndReturnOkWithCodeMatched()
    {
        _mediator.Send(new ValidateIfoodPickupCodeCommand(1, "1234"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ValidateIfoodPickupCode(1, new ValidateIfoodPickupCodeRequest("1234"), CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { codeMatched = true });
    }

    [Fact]
    public async Task ValidateIfoodPickupCode_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ValidateIfoodPickupCodeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ValidateIfoodPickupCode(999, new ValidateIfoodPickupCodeRequest("1234"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Disputes ----

    [Fact]
    public async Task AcceptIfoodDispute_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new AcceptIfoodDisputeCommand(1, "dispute-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IfoodDisputeActionResponse(true, "ACCEPTED")));

        var result = await _controller.AcceptIfoodDispute("dispute-1", new IfoodDisputeActionRequest(1), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AcceptIfoodDispute_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<AcceptIfoodDisputeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodDisputeActionResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.AcceptIfoodDispute("dispute-999", new IfoodDisputeActionRequest(999), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RejectIfoodDispute_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new RejectIfoodDisputeCommand(1, "dispute-1", "motivo"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IfoodDisputeActionResponse(true, "REJECTED")));

        var result = await _controller.RejectIfoodDispute("dispute-1", new RejectIfoodDisputeRequest(1, "motivo"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RejectIfoodDispute_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RejectIfoodDisputeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodDisputeActionResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.RejectIfoodDispute("dispute-999", new RejectIfoodDisputeRequest(999, "motivo"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RequestIfoodDisputeAlternative_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new RequestIfoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "REFUND", 10m, "BRL"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IfoodDisputeActionResponse(true, "REQUESTED")));

        var result = await _controller.RequestIfoodDisputeAlternative(
            "dispute-1", "alt-1", new RequestIfoodDisputeAlternativeRequest(1, "REFUND", 10m, "BRL"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RequestIfoodDisputeAlternative_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RequestIfoodDisputeAlternativeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodDisputeActionResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.RequestIfoodDisputeAlternative(
            "dispute-999", "alt-999", new RequestIfoodDisputeAlternativeRequest(999, "REFUND", null, null), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOrderVirtualBag_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodOrderVirtualBagQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodOrderVirtualBagResponse)null!));

        var result = await _controller.GetIfoodOrderVirtualBag(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOrderVirtualBag_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodOrderVirtualBagQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodOrderVirtualBagResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodOrderVirtualBag(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RequestIfoodOrderDriver_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new RequestIfoodOrderDriverCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.RequestIfoodOrderDriver(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RequestIfoodOrderDriver_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RequestIfoodOrderDriverCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.RequestIfoodOrderDriver(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelIfoodOrderDriverRequest_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new CancelIfoodOrderDriverRequestCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.CancelIfoodOrderDriverRequest(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelIfoodOrderDriverRequest_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CancelIfoodOrderDriverRequestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.CancelIfoodOrderDriverRequest(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task VerifyIfoodOrderDeliveryCode_Success_ShouldSendCommandAndReturnOkWithCodeMatched()
    {
        _mediator.Send(new VerifyIfoodOrderDeliveryCodeCommand(1, "1234"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.VerifyIfoodOrderDeliveryCode(1, new VerifyIfoodOrderDeliveryCodeRequest("1234"), CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { codeMatched = true });
    }

    [Fact]
    public async Task VerifyIfoodOrderDeliveryCode_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<VerifyIfoodOrderDeliveryCodeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.VerifyIfoodOrderDeliveryCode(999, new VerifyIfoodOrderDeliveryCodeRequest("1234"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Catalog ----

    [Fact]
    public async Task SyncIfoodCatalog_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new SyncIfoodCatalogCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodCatalogSyncSummary)null!));

        var result = await _controller.SyncIfoodCatalog(new SyncIfoodCatalogRequest(1), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SyncIfoodCatalog_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<SyncIfoodCatalogCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodCatalogSyncSummary>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SyncIfoodCatalog(new SyncIfoodCatalogRequest(999), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodCatalogs_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodCatalogsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodCatalogSummaryResponse>>([]));

        var result = await _controller.GetIfoodCatalogs(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodCatalogs_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodCatalogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodCatalogSummaryResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodCatalogs(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListIfoodCategories_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new ListIfoodCategoriesQuery(1, "catalog-1", true), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodCategoryResponse>>([]));

        var result = await _controller.ListIfoodCategories(1, "catalog-1", true, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListIfoodCategories_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ListIfoodCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodCategoryResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ListIfoodCategories(999, "catalog-1", true, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodCategory_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodCategoryQuery(1, "catalog-1", "categ-1", true), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodCategoryResponse)null!));

        var result = await _controller.GetIfoodCategory(1, "catalog-1", "categ-1", true, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodCategory_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodCategoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodCategoryResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodCategory(999, "catalog-1", "categ-1", true, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateIfoodCategory_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new CreateIfoodCategoryCommand(1, "catalog-1", "Bebidas"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IfoodCategoryCreateResponse("categ-1")));

        var result = await _controller.CreateIfoodCategory(1, "catalog-1", new CreateIfoodCategoryRequest("Bebidas"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateIfoodCategory_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CreateIfoodCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodCategoryCreateResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.CreateIfoodCategory(999, "catalog-1", new CreateIfoodCategoryRequest("Bebidas"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EditIfoodCategory_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new EditIfoodCategoryCommand(1, "catalog-1", "categ-1", "Bebidas", "ext-1", "AVAILABLE", 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodCategoryResponse)null!));

        var result = await _controller.EditIfoodCategory(
            1, "catalog-1", "categ-1", new EditIfoodCategoryRequest("Bebidas", "ext-1", "AVAILABLE", 1), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task EditIfoodCategory_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<EditIfoodCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodCategoryResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.EditIfoodCategory(
            999, "catalog-1", "categ-1", new EditIfoodCategoryRequest("Bebidas", "ext-1", "AVAILABLE", 1), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteIfoodCategory_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new DeleteIfoodCategoryCommand(1, "categ-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DeleteIfoodCategory(1, "categ-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteIfoodCategory_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteIfoodCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DeleteIfoodCategory(999, "categ-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListIfoodSellableItems_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new ListIfoodSellableItemsQuery(1, "group-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodSellableItemResponse>>([]));

        var result = await _controller.ListIfoodSellableItems(1, "group-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListIfoodSellableItems_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ListIfoodSellableItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodSellableItemResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ListIfoodSellableItems(999, "group-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListIfoodProducts_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new ListIfoodProductsQuery(1, 10, 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodProductResponse>>([]));

        var result = await _controller.ListIfoodProducts(1, 10, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListIfoodProducts_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ListIfoodProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodProductResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ListIfoodProducts(999, null, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateIfoodProduct_Success_ShouldSendCommandAndReturnOk()
    {
        var request = new CreateIfoodProductRequest("prod-1", "Produto", "descricao", "info adicional", "ext-1", "789", "img.png", null);
        _mediator.Send(
                new CreateIfoodProductCommand(1, "prod-1", "Produto", "descricao", "info adicional", "ext-1", "789", "img.png", null),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodProductResponse)null!));

        var result = await _controller.CreateIfoodProduct(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateIfoodProduct_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new CreateIfoodProductRequest("prod-999", "Produto", null, null, null, null, null, null);
        _mediator.Send(Arg.Any<CreateIfoodProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodProductResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.CreateIfoodProduct(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EditIfoodProduct_Success_ShouldSendCommandAndReturnOk()
    {
        var productId = Guid.NewGuid();
        var request = new EditIfoodProductRequest("Produto", "descricao", "info adicional", "ext-1", "789", "img.png", null);
        _mediator.Send(
                new EditIfoodProductCommand(1, productId, "Produto", "descricao", "info adicional", "ext-1", "789", "img.png", null),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodProductResponse)null!));

        var result = await _controller.EditIfoodProduct(1, productId, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task EditIfoodProduct_Failure_ShouldReturnMappedErrorResult()
    {
        var productId = Guid.NewGuid();
        var request = new EditIfoodProductRequest("Produto", null, null, null, null, null, null);
        _mediator.Send(Arg.Any<EditIfoodProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodProductResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.EditIfoodProduct(999, productId, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteIfoodProduct_Success_ShouldSendCommandAndReturnNoContent()
    {
        var productId = Guid.NewGuid();
        _mediator.Send(new DeleteIfoodProductCommand(1, productId), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DeleteIfoodProduct(1, productId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteIfoodProduct_Failure_ShouldReturnMappedErrorResult()
    {
        var productId = Guid.NewGuid();
        _mediator.Send(Arg.Any<DeleteIfoodProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DeleteIfoodProduct(999, productId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task BatchUpdateIfoodProductStatuses_Success_ShouldSendCommandAndReturnNoContent()
    {
        var items = new List<IfoodBatchProductStatusInput> { new("prod-1", "ext-1", "AVAILABLE", null) };
        var request = new BatchUpdateIfoodProductStatusesRequest(items, "catalog-1");
        _mediator.Send(new BatchUpdateIfoodProductStatusesCommand(1, items, "catalog-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.BatchUpdateIfoodProductStatuses(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task BatchUpdateIfoodProductStatuses_Failure_ShouldReturnMappedErrorResult()
    {
        var items = new List<IfoodBatchProductStatusInput> { new("prod-1", "ext-1", "PAUSED", null) };
        var request = new BatchUpdateIfoodProductStatusesRequest(items, "catalog-1");
        _mediator.Send(Arg.Any<BatchUpdateIfoodProductStatusesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.BatchUpdateIfoodProductStatuses(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task BatchUpdateIfoodProductPrices_Success_ShouldSendCommandAndReturnOk()
    {
        var items = new List<IfoodBatchProductPriceInput> { new("prod-1", "ext-1", 10m, null, null) };
        var request = new BatchUpdateIfoodProductPricesRequest(items, "catalog-1");
        _mediator.Send(new BatchUpdateIfoodProductPricesCommand(1, items, "catalog-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IfoodBatchDispatchResponse("https://batch", "batch-1")));

        var result = await _controller.BatchUpdateIfoodProductPrices(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task BatchUpdateIfoodProductPrices_Failure_ShouldReturnMappedErrorResult()
    {
        var items = new List<IfoodBatchProductPriceInput> { new("prod-1", "ext-1", 10m, null, null) };
        var request = new BatchUpdateIfoodProductPricesRequest(items, "catalog-1");
        _mediator.Send(Arg.Any<BatchUpdateIfoodProductPricesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodBatchDispatchResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.BatchUpdateIfoodProductPrices(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListIfoodProductsByExternalCode_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new ListIfoodProductsByExternalCodeQuery(1, "ext-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodProductResponse>>([]));

        var result = await _controller.ListIfoodProductsByExternalCode(1, "ext-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListIfoodProductsByExternalCode_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ListIfoodProductsByExternalCodeQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodProductResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ListIfoodProductsByExternalCode(999, "ext-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodProductById_Success_ShouldSendQueryAndReturnOk()
    {
        var productId = Guid.NewGuid();
        _mediator.Send(new GetIfoodProductByIdQuery(1, productId), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodProductResponse)null!));

        var result = await _controller.GetIfoodProductById(1, productId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodProductById_Failure_ShouldReturnMappedErrorResult()
    {
        var productId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetIfoodProductByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodProductResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodProductById(999, productId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodItemFlat_Success_ShouldSendQueryAndReturnOk()
    {
        var itemId = Guid.NewGuid();
        _mediator.Send(new GetIfoodItemFlatQuery(1, itemId), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodItemFlatResponse)null!));

        var result = await _controller.GetIfoodItemFlat(1, itemId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodItemFlat_Failure_ShouldReturnMappedErrorResult()
    {
        var itemId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetIfoodItemFlatQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodItemFlatResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodItemFlat(999, itemId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetIfoodItemPrice_Success_ShouldSendCommandAndReturnNoContent()
    {
        var itemId = Guid.NewGuid();
        _mediator.Send(new SetIfoodItemPriceCommand(1, itemId, 10m, 12m, null), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.SetIfoodItemPrice(1, itemId, new SetIfoodItemPriceRequest(10m, 12m, null), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetIfoodItemPrice_Failure_ShouldReturnMappedErrorResult()
    {
        var itemId = Guid.NewGuid();
        _mediator.Send(Arg.Any<SetIfoodItemPriceCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SetIfoodItemPrice(999, itemId, new SetIfoodItemPriceRequest(10m, null, null), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetIfoodItemExternalCode_Success_ShouldSendCommandAndReturnNoContent()
    {
        var itemId = Guid.NewGuid();
        _mediator.Send(new SetIfoodItemExternalCodeCommand(1, itemId, "ext-1", null), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.SetIfoodItemExternalCode(1, itemId, new SetIfoodItemExternalCodeRequest("ext-1", null), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetIfoodItemExternalCode_Failure_ShouldReturnMappedErrorResult()
    {
        var itemId = Guid.NewGuid();
        _mediator.Send(Arg.Any<SetIfoodItemExternalCodeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SetIfoodItemExternalCode(999, itemId, new SetIfoodItemExternalCodeRequest("ext-1", null), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteIfoodItem_Success_ShouldSendCommandAndReturnNoContent()
    {
        var productId = Guid.NewGuid();
        _mediator.Send(new DeleteIfoodItemCommand(1, "categ-1", productId, "catalog-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DeleteIfoodItem(1, "categ-1", productId, "catalog-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteIfoodItem_Failure_ShouldReturnMappedErrorResult()
    {
        var productId = Guid.NewGuid();
        _mediator.Send(Arg.Any<DeleteIfoodItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DeleteIfoodItem(999, "categ-1", productId, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListIfoodCategoryItems_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new ListIfoodCategoryItemsQuery(1, "categ-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodCategoryItemsResponse)null!));

        var result = await _controller.ListIfoodCategoryItems(1, "categ-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListIfoodCategoryItems_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ListIfoodCategoryItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodCategoryItemsResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ListIfoodCategoryItems(999, "categ-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListIfoodOptionGroups_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new ListIfoodOptionGroupsQuery(1, true, "catalog-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodOptionGroupResponse>>([]));

        var result = await _controller.ListIfoodOptionGroups(1, true, "catalog-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListIfoodOptionGroups_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ListIfoodOptionGroupsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodOptionGroupResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ListIfoodOptionGroups(999, true, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateIfoodOptionGroup_Success_ShouldSendCommandAndReturnNoContent()
    {
        var optionGroupId = Guid.NewGuid();
        _mediator.Send(new UpdateIfoodOptionGroupCommand(1, optionGroupId, "Tamanhos"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UpdateIfoodOptionGroup(1, optionGroupId, new UpdateIfoodOptionGroupRequest("Tamanhos"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateIfoodOptionGroup_Failure_ShouldReturnMappedErrorResult()
    {
        var optionGroupId = Guid.NewGuid();
        _mediator.Send(Arg.Any<UpdateIfoodOptionGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.UpdateIfoodOptionGroup(999, optionGroupId, new UpdateIfoodOptionGroupRequest("Tamanhos"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteIfoodOptionGroup_Success_ShouldSendCommandAndReturnNoContent()
    {
        var optionGroupId = Guid.NewGuid();
        _mediator.Send(new DeleteIfoodOptionGroupCommand(1, optionGroupId), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DeleteIfoodOptionGroup(1, optionGroupId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteIfoodOptionGroup_Failure_ShouldReturnMappedErrorResult()
    {
        var optionGroupId = Guid.NewGuid();
        _mediator.Send(Arg.Any<DeleteIfoodOptionGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DeleteIfoodOptionGroup(999, optionGroupId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DisassociateIfoodOptionGroup_Success_ShouldSendCommandAndReturnNoContent()
    {
        var optionGroupId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _mediator.Send(new DisassociateIfoodOptionGroupCommand(1, optionGroupId, productId), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DisassociateIfoodOptionGroup(1, optionGroupId, productId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DisassociateIfoodOptionGroup_Failure_ShouldReturnMappedErrorResult()
    {
        var optionGroupId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _mediator.Send(Arg.Any<DisassociateIfoodOptionGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DisassociateIfoodOptionGroup(999, optionGroupId, productId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteIfoodOption_Success_ShouldSendCommandAndReturnNoContent()
    {
        var optionGroupId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _mediator.Send(new DeleteIfoodOptionCommand(1, optionGroupId, productId, "catalog-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DeleteIfoodOption(1, optionGroupId, productId, "catalog-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteIfoodOption_Failure_ShouldReturnMappedErrorResult()
    {
        var optionGroupId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _mediator.Send(Arg.Any<DeleteIfoodOptionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DeleteIfoodOption(999, optionGroupId, productId, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateIfoodOptionGroupStatus_Success_ShouldSendCommandAndReturnNoContent()
    {
        var optionGroupId = Guid.NewGuid();
        _mediator.Send(new UpdateIfoodOptionGroupStatusCommand(1, optionGroupId, true), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UpdateIfoodOptionGroupStatus(1, optionGroupId, new UpdateIfoodOptionGroupStatusRequest(true), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateIfoodOptionGroupStatus_Failure_ShouldReturnMappedErrorResult()
    {
        var optionGroupId = Guid.NewGuid();
        _mediator.Send(Arg.Any<UpdateIfoodOptionGroupStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.UpdateIfoodOptionGroupStatus(999, optionGroupId, new UpdateIfoodOptionGroupStatusRequest(true), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetIfoodOptionPrice_Success_ShouldSendCommandAndReturnNoContent()
    {
        var optionId = Guid.NewGuid();
        _mediator.Send(new SetIfoodOptionPriceCommand(1, optionId, 5m, 6m, null), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.SetIfoodOptionPrice(1, optionId, new SetIfoodOptionPriceRequest(5m, 6m, null), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetIfoodOptionPrice_Failure_ShouldReturnMappedErrorResult()
    {
        var optionId = Guid.NewGuid();
        _mediator.Send(Arg.Any<SetIfoodOptionPriceCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SetIfoodOptionPrice(999, optionId, new SetIfoodOptionPriceRequest(5m, null, null), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetIfoodOptionExternalCode_Success_ShouldSendCommandAndReturnNoContent()
    {
        var optionId = Guid.NewGuid();
        _mediator.Send(new SetIfoodOptionExternalCodeCommand(1, optionId, "ext-1", null), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.SetIfoodOptionExternalCode(1, optionId, new SetIfoodOptionExternalCodeRequest("ext-1", null), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetIfoodOptionExternalCode_Failure_ShouldReturnMappedErrorResult()
    {
        var optionId = Guid.NewGuid();
        _mediator.Send(Arg.Any<SetIfoodOptionExternalCodeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SetIfoodOptionExternalCode(999, optionId, new SetIfoodOptionExternalCodeRequest("ext-1", null), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetIfoodOptionStatus_Success_ShouldSendCommandAndReturnNoContent()
    {
        var optionId = Guid.NewGuid();
        _mediator.Send(new SetIfoodOptionStatusCommand(1, optionId, true, null), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.SetIfoodOptionStatus(1, optionId, new SetIfoodOptionStatusRequest(true, null), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetIfoodOptionStatus_Failure_ShouldReturnMappedErrorResult()
    {
        var optionId = Guid.NewGuid();
        _mediator.Send(Arg.Any<SetIfoodOptionStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SetIfoodOptionStatus(999, optionId, new SetIfoodOptionStatusRequest(true, null), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodInventory_Success_ShouldSendQueryAndReturnOk()
    {
        var productId = Guid.NewGuid();
        _mediator.Send(new GetIfoodInventoryQuery(1, productId), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodInventoryResponse)null!));

        var result = await _controller.GetIfoodInventory(1, productId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodInventory_Failure_ShouldReturnMappedErrorResult()
    {
        var productId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetIfoodInventoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodInventoryResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodInventory(999, productId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteIfoodInventoryBatch_Success_ShouldSendCommandAndReturnNoContent()
    {
        var productIds = new List<Guid> { Guid.NewGuid() };
        _mediator.Send(new DeleteIfoodInventoryBatchCommand(1, productIds), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DeleteIfoodInventoryBatch(1, new DeleteIfoodInventoryBatchRequest(productIds), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteIfoodInventoryBatch_Failure_ShouldReturnMappedErrorResult()
    {
        var productIds = new List<Guid> { Guid.NewGuid() };
        _mediator.Send(Arg.Any<DeleteIfoodInventoryBatchCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DeleteIfoodInventoryBatch(999, new DeleteIfoodInventoryBatchRequest(productIds), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodBatchResult_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodBatchResultQuery(1, "batch-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodBatchStatusResponse)null!));

        var result = await _controller.GetIfoodBatchResult(1, "batch-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodBatchResult_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodBatchResultQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodBatchStatusResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodBatchResult(999, "batch-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CheckIfoodCatalogVersion_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new CheckIfoodCatalogVersionQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodCatalogVersionResponse)null!));

        var result = await _controller.CheckIfoodCatalogVersion(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CheckIfoodCatalogVersion_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CheckIfoodCatalogVersionQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodCatalogVersionResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.CheckIfoodCatalogVersion(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpgradeIfoodCatalogVersion_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new UpgradeIfoodCatalogVersionCommand(1, true), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UpgradeIfoodCatalogVersion(1, new UpgradeIfoodCatalogVersionRequest(true), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpgradeIfoodCatalogVersion_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<UpgradeIfoodCatalogVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.UpgradeIfoodCatalogVersion(999, new UpgradeIfoodCatalogVersionRequest(true), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DowngradeIfoodCatalogVersion_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new DowngradeIfoodCatalogVersionCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DowngradeIfoodCatalogVersion(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DowngradeIfoodCatalogVersion_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DowngradeIfoodCatalogVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DowngradeIfoodCatalogVersion(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UploadIfoodImage_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new UploadIfoodImageCommand(1, "{}"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodImageUploadResponse)null!));

        var result = await _controller.UploadIfoodImage(1, new UploadIfoodImageRequest("{}"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UploadIfoodImage_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<UploadIfoodImageCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodImageUploadResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.UploadIfoodImage(999, new UploadIfoodImageRequest("{}"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task InvokeIfoodCatalogV1Operation_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new InvokeIfoodCatalogV1OperationCommand(1, IfoodCatalogV1Operation.ListCatalogs, null, null, null), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodCatalogV1OperationResponse)null!));

        var result = await _controller.InvokeIfoodCatalogV1Operation(
            1, new InvokeIfoodCatalogV1OperationRequest(IfoodCatalogV1Operation.ListCatalogs, null, null, null), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task InvokeIfoodCatalogV1Operation_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<InvokeIfoodCatalogV1OperationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodCatalogV1OperationResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.InvokeIfoodCatalogV1Operation(
            999, new InvokeIfoodCatalogV1OperationRequest(IfoodCatalogV1Operation.ListCatalogs, null, null, null), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Financial ----

    [Fact]
    public async Task GetIfoodFinancialSummary_Success_ShouldSendQueryAndReturnOk()
    {
        var from = DateTime.Today.AddDays(-30);
        var to = DateTime.Today;
        _mediator.Send(new GetIfoodFinancialSummaryQuery(1, from, to), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodFinancialSummaryResponse)null!));

        var result = await _controller.GetIfoodFinancialSummary(1, from, to, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodFinancialSummary_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodFinancialSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodFinancialSummaryResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodFinancialSummary(999, null, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SyncIfoodFinancial_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new SyncIfoodFinancialCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.SyncIfoodFinancial(new SyncIfoodFinancialRequest(1), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SyncIfoodFinancial_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<SyncIfoodFinancialCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SyncIfoodFinancial(new SyncIfoodFinancialRequest(999), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodFinancialReport_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodFinancialReportQuery(1, IfoodFinancialReportType.SalesV3, "period-1", null, null), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodFinancialReportResponse)null!));

        var result = await _controller.GetIfoodFinancialReport(1, IfoodFinancialReportType.SalesV3, "period-1", null, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodFinancialReport_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodFinancialReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodFinancialReportResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodFinancialReport(999, IfoodFinancialReportType.SalesV3, null, null, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RequestIfoodReconciliationOnDemand_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new RequestIfoodReconciliationOnDemandCommand(1, "2026-01"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodReconciliationOnDemandResponse)null!));

        var result = await _controller.RequestIfoodReconciliationOnDemand(1, new RequestIfoodReconciliationOnDemandRequest("2026-01"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RequestIfoodReconciliationOnDemand_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RequestIfoodReconciliationOnDemandCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodReconciliationOnDemandResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.RequestIfoodReconciliationOnDemand(999, new RequestIfoodReconciliationOnDemandRequest("2026-01"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodReconciliationOnDemandStatus_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodReconciliationOnDemandStatusQuery(1, "req-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodReconciliationOnDemandStatusResponse)null!));

        var result = await _controller.GetIfoodReconciliationOnDemandStatus(1, "req-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodReconciliationOnDemandStatus_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodReconciliationOnDemandStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodReconciliationOnDemandStatusResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodReconciliationOnDemandStatus(999, "req-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Merchant ----

    [Fact]
    public async Task GetIfoodMerchantStatus_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodMerchantStatusQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodMerchantStatusResponse)null!));

        var result = await _controller.GetIfoodMerchantStatus(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodMerchantStatus_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodMerchantStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodMerchantStatusResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodMerchantStatus(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodInterruptions_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodInterruptionsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodInterruptionResponse>>([]));

        var result = await _controller.GetIfoodInterruptions(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodInterruptions_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodInterruptionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodInterruptionResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodInterruptions(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateIfoodInterruption_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new CreateIfoodInterruptionCommand(1, "Manutencao", DateTime.Today, DateTime.Today.AddHours(2));
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.CreateIfoodInterruption(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CreateIfoodInterruption_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateIfoodInterruptionCommand(999, "Manutencao", DateTime.Today, DateTime.Today.AddHours(2));
        _mediator.Send(Arg.Any<CreateIfoodInterruptionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.CreateIfoodInterruption(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteIfoodInterruption_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new DeleteIfoodInterruptionCommand(1, "interrupt-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DeleteIfoodInterruption("interrupt-1", 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteIfoodInterruption_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteIfoodInterruptionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DeleteIfoodInterruption("interrupt-999", 999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOpeningHours_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodOpeningHoursQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodOpeningHoursResponse)null!));

        var result = await _controller.GetIfoodOpeningHours(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOpeningHours_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodOpeningHoursQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodOpeningHoursResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodOpeningHours(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SaveIfoodOpeningHours_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "08:00", 600)]);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SaveIfoodOpeningHours(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SaveIfoodOpeningHours_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SaveIfoodOpeningHoursCommand(999, [new IfoodOpeningHourShiftInput(1, "08:00", 600)]);
        _mediator.Send(Arg.Any<SaveIfoodOpeningHoursCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SaveIfoodOpeningHours(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetIfoodPreparationTime_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new SetIfoodPreparationTimeCommand(1, 15);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetIfoodPreparationTime(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetIfoodPreparationTime_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SetIfoodPreparationTimeCommand(999, null);
        _mediator.Send(Arg.Any<SetIfoodPreparationTimeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.SetIfoodPreparationTime(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodMerchantsList_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodMerchantsListQuery(1, 2, 50), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodMerchantSummaryResponse>>([]));

        var result = await _controller.GetIfoodMerchantsList(1, 2, 50, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodMerchantsList_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodMerchantsListQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodMerchantSummaryResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodMerchantsList(999, 0, 0, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodMerchantDetails_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodMerchantDetailsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodMerchantDetailsResponse)null!));

        var result = await _controller.GetIfoodMerchantDetails(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodMerchantDetails_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodMerchantDetailsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodMerchantDetailsResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodMerchantDetails(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodMerchantStatusByOperation_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodMerchantStatusByOperationQuery(1, "OPEN"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodMerchantStatusByOperationResponse)null!));

        var result = await _controller.GetIfoodMerchantStatusByOperation(1, "OPEN", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodMerchantStatusByOperation_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodMerchantStatusByOperationQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodMerchantStatusByOperationResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodMerchantStatusByOperation(999, "OPEN", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Logistics ----

    [Fact]
    public async Task GetIfoodLogisticsDeliveries_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodLogisticsDeliveriesQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodLogisticsDeliveryResponse>>([]));

        var result = await _controller.GetIfoodLogisticsDeliveries(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodLogisticsDeliveries_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodLogisticsDeliveriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodLogisticsDeliveryResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodLogisticsDeliveries(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AssignIfoodDriver_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new AssignIfoodDriverCommand(1, "Joao", "11999999999", "MOTORCYCLE"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.AssignIfoodDriver(1, new AssignIfoodDriverRequest("Joao", "11999999999", "MOTORCYCLE"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AssignIfoodDriver_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<AssignIfoodDriverCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.AssignIfoodDriver(999, new AssignIfoodDriverRequest("Joao", "11999999999", "MOTORCYCLE"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkIfoodGoingToOrigin_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new MarkIfoodGoingToOriginCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.MarkIfoodGoingToOrigin(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkIfoodGoingToOrigin_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<MarkIfoodGoingToOriginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.MarkIfoodGoingToOrigin(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkIfoodArrivedAtOrigin_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new MarkIfoodArrivedAtOriginCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.MarkIfoodArrivedAtOrigin(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkIfoodArrivedAtOrigin_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<MarkIfoodArrivedAtOriginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.MarkIfoodArrivedAtOrigin(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DispatchIfoodLogistics_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new DispatchIfoodLogisticsCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DispatchIfoodLogistics(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DispatchIfoodLogistics_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DispatchIfoodLogisticsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DispatchIfoodLogistics(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkIfoodArrivedAtDestination_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new MarkIfoodArrivedAtDestinationCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.MarkIfoodArrivedAtDestination(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkIfoodArrivedAtDestination_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<MarkIfoodArrivedAtDestinationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.MarkIfoodArrivedAtDestination(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task VerifyIfoodDeliveryCode_Success_ShouldSendCommandAndReturnOkWithCodeMatched()
    {
        _mediator.Send(new VerifyIfoodDeliveryCodeCommand(1, "1234"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.VerifyIfoodDeliveryCode(1, new VerifyIfoodDeliveryCodeRequest("1234"), CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { codeMatched = true });
    }

    [Fact]
    public async Task VerifyIfoodDeliveryCode_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<VerifyIfoodDeliveryCodeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.VerifyIfoodDeliveryCode(999, new VerifyIfoodDeliveryCodeRequest("1234"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodLogisticsOrderDetails_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodLogisticsOrderDetailsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodLogisticsOrderDetailsResponse)null!));

        var result = await _controller.GetIfoodLogisticsOrderDetails(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodLogisticsOrderDetails_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodLogisticsOrderDetailsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodLogisticsOrderDetailsResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodLogisticsOrderDetails(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Shipping ----

    [Fact]
    public async Task GetIfoodShippingDeliveries_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodShippingDeliveriesQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodShippingDeliveryResponse>>([]));

        var result = await _controller.GetIfoodShippingDeliveries(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodShippingDeliveries_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodShippingDeliveriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodShippingDeliveryResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodShippingDeliveries(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodShippingQuote_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodShippingQuoteQuery(1, -23.5, -46.6), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodShippingQuoteResponse)null!));

        var result = await _controller.GetIfoodShippingQuote(1, -23.5, -46.6, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodShippingQuote_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodShippingQuoteQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodShippingQuoteResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodShippingQuote(999, -23.5, -46.6, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RequestIfoodShippingDriver_Success_ShouldSendCommandAndReturnOkWithId()
    {
        var command = new RequestIfoodShippingDriverCommand(
            1, "ref-1", "Cliente", "11", "999999999", 5m, "quote-1", "01000-000", "100", "Rua A", null, "Centro", "Sao Paulo", "SP", "BR", null, null, null,
            [new IfoodShippingItemInput("Produto", "ext-1", 1, 10m)]);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(123L));

        var result = await _controller.RequestIfoodShippingDriver(command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { id = 123L });
    }

    [Fact]
    public async Task RequestIfoodShippingDriver_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new RequestIfoodShippingDriverCommand(
            999, "ref-1", "Cliente", "11", "999999999", 5m, "quote-1", "01000-000", "100", "Rua A", null, "Centro", "Sao Paulo", "SP", "BR", null, null, null,
            [new IfoodShippingItemInput("Produto", "ext-1", 1, 10m)]);
        _mediator.Send(Arg.Any<RequestIfoodShippingDriverCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.RequestIfoodShippingDriver(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodShippingTracking_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodShippingTrackingQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodShippingTrackingResponse)null!));

        var result = await _controller.GetIfoodShippingTracking(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodShippingTracking_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodShippingTrackingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodShippingTrackingResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodShippingTracking(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodShippingCancellationReasons_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodShippingCancellationReasonsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodShippingCancellationReasonResponse>>([]));

        var result = await _controller.GetIfoodShippingCancellationReasons(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodShippingCancellationReasons_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodShippingCancellationReasonsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodShippingCancellationReasonResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodShippingCancellationReasons(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelIfoodShippingDelivery_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new CancelIfoodShippingDeliveryCommand(1, "motivo", 100), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.CancelIfoodShippingDelivery(1, new CancelIfoodShippingDeliveryRequest("motivo", 100), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelIfoodShippingDelivery_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CancelIfoodShippingDeliveryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.CancelIfoodShippingDelivery(999, new CancelIfoodShippingDeliveryRequest("motivo", 100), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodSafeDeliveryScore_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodSafeDeliveryScoreQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodSafeDeliveryScoreResponse)null!));

        var result = await _controller.GetIfoodSafeDeliveryScore(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodSafeDeliveryScore_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodSafeDeliveryScoreQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodSafeDeliveryScoreResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodSafeDeliveryScore(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOrderShippingQuote_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodOrderShippingQuoteQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodShippingQuoteResponse)null!));

        var result = await _controller.GetIfoodOrderShippingQuote(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOrderShippingQuote_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodOrderShippingQuoteQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodShippingQuoteResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodOrderShippingQuote(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RequestIfoodOrderShippingDriver_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new RequestIfoodOrderShippingDriverCommand(1, "quote-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.RequestIfoodOrderShippingDriver(1, new RequestIfoodOrderShippingDriverRequest("quote-1"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RequestIfoodOrderShippingDriver_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RequestIfoodOrderShippingDriverCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.RequestIfoodOrderShippingDriver(999, new RequestIfoodOrderShippingDriverRequest("quote-1"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelIfoodOrderShippingDriver_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new CancelIfoodOrderShippingDriverCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.CancelIfoodOrderShippingDriver(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelIfoodOrderShippingDriver_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CancelIfoodOrderShippingDriverCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.CancelIfoodOrderShippingDriver(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RequestIfoodDeliveryAddressChange_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new RequestIfoodDeliveryAddressChangeRequest(
            "100", "Rua A", null, "Centro", "Sao Paulo", "SP", "BR", null, -23.5, -46.6);
        _mediator.Send(
                new RequestDeliveryAddressChangeCommand(1, "100", "Rua A", null, "Centro", "Sao Paulo", "SP", "BR", null, -23.5, -46.6),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.RequestIfoodDeliveryAddressChange(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RequestIfoodDeliveryAddressChange_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new RequestIfoodDeliveryAddressChangeRequest(
            "100", "Rua A", null, "Centro", "Sao Paulo", "SP", "BR", null, null, null);
        _mediator.Send(Arg.Any<RequestDeliveryAddressChangeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.RequestIfoodDeliveryAddressChange(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AcceptIfoodDeliveryAddressChange_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new AcceptDeliveryAddressChangeCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.AcceptIfoodDeliveryAddressChange(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AcceptIfoodDeliveryAddressChange_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<AcceptDeliveryAddressChangeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.AcceptIfoodDeliveryAddressChange(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DenyIfoodDeliveryAddressChange_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new DenyDeliveryAddressChangeCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.DenyIfoodDeliveryAddressChange(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DenyIfoodDeliveryAddressChange_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DenyDeliveryAddressChangeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.DenyIfoodDeliveryAddressChange(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ConfirmIfoodUserAddress_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(new ConfirmUserAddressCommand(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.ConfirmIfoodUserAddress(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ConfirmIfoodUserAddress_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ConfirmUserAddressCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ConfirmIfoodUserAddress(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Reviews ----

    [Fact]
    public async Task GetIfoodReviews_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodReviewsQuery(1, 2, 20, null, null, "ASC", "RATING"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodReviewListResponse)null!));

        var result = await _controller.GetIfoodReviews(1, 2, 20, null, null, "ASC", "RATING", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodReviews_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodReviewsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodReviewListResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodReviews(999, 1, 10, null, null, null, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodReviewById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodReviewByIdQuery(1, "review-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodReviewDetailResponse)null!));

        var result = await _controller.GetIfoodReviewById(1, "review-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodReviewById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodReviewByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodReviewDetailResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodReviewById(999, "review-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReplyIfoodReview_Success_ShouldSendCommandAndReturnOk()
    {
        _mediator.Send(new ReplyIfoodReviewCommand(1, "review-1", "obrigado"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IfoodReviewReplyResponse(DateTime.UtcNow, "obrigado", "review-1")));

        var result = await _controller.ReplyIfoodReview(1, "review-1", new ReplyIfoodReviewRequest("obrigado"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ReplyIfoodReview_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ReplyIfoodReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodReviewReplyResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.ReplyIfoodReview(999, "review-1", new ReplyIfoodReviewRequest("obrigado"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetIfoodReviewsSummary_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodReviewsSummaryQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodReviewSummaryResponse)null!));

        var result = await _controller.GetIfoodReviewsSummary(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodReviewsSummary_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodReviewsSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodReviewSummaryResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodReviewsSummary(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Analytics ----

    [Fact]
    public async Task GetIfoodOrderKpis_Success_ShouldSendQueryAndReturnOk()
    {
        var periodStart = DateTime.Today.AddDays(-7);
        var periodEnd = DateTime.Today;
        _mediator.Send(new GetIfoodOrderKpisQuery(1, periodStart, periodEnd, 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((IfoodOrderKpisResponse)null!));

        var result = await _controller.GetIfoodOrderKpis(1, periodStart, periodEnd, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOrderKpis_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodOrderKpisQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IfoodOrderKpisResponse>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodOrderKpis(999, null, null, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ---- Alerts ----

    [Fact]
    public async Task GetIfoodOperationalAlerts_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(new GetIfoodOperationalAlertsQuery(1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<IfoodOperationalAlertResponse>>([]));

        var result = await _controller.GetIfoodOperationalAlerts(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetIfoodOperationalAlerts_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetIfoodOperationalAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<IfoodOperationalAlertResponse>>(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.GetIfoodOperationalAlerts(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AcknowledgeIfoodOperationalAlert_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new AcknowledgeIfoodOperationalAlertCommand(1, Guid.NewGuid());
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.AcknowledgeIfoodOperationalAlert(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AcknowledgeIfoodOperationalAlert_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new AcknowledgeIfoodOperationalAlertCommand(999, Guid.NewGuid());
        _mediator.Send(Arg.Any<AcknowledgeIfoodOperationalAlertCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Ifood.NotFound", "nao encontrado")));

        var result = await _controller.AcknowledgeIfoodOperationalAlert(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
