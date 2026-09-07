using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.AcceptRefund;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.ConfirmOrder;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.DispatchOrder;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkDelivered;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkReadyForPickup;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RejectRefund;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RequestCancellation;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.SendTrackingUpdate;
using SyncBar.Application.Features.Integrations.Keeta.Order.Create;
using SyncBar.Application.Features.Integrations.Keeta.Order.Delete;
using SyncBar.Application.Features.Integrations.Keeta.Order.ExistsByKeetaOrderId;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetActiveOrdersByBranch;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByBranchId;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByCompanyId;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetByDisplayId;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetByKeetaOrderId;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
using SyncBar.Application.Features.Integrations.Keeta.Order.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaOrderControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly KeetaOrderController _controller;

    public KeetaOrderControllerTests()
    {
        _controller = new KeetaOrderController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaOrderByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationOrderResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaOrderByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationOrderResponse>(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByKeetaOrderId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaOrderByKeetaOrderIdQuery>(q => q.KeetaOrderId == "ko_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationOrderResponse)null!));

        var result = await _controller.GetByKeetaOrderId("ko_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByKeetaOrderId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaOrderByKeetaOrderIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationOrderResponse>(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.GetByKeetaOrderId("ko_missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByDisplayId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaOrderByDisplayIdQuery>(q => q.DisplayId == "1234"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationOrderResponse)null!));

        var result = await _controller.GetByDisplayId("1234", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByDisplayId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaOrderByDisplayIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationOrderResponse>(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.GetByDisplayId("9999", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAllByCompanyId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAllKeetaOrdersByCompanyIdQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationOrderResponse>>([]));

        var result = await _controller.GetAllByCompanyId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllByCompanyId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAllKeetaOrdersByCompanyIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationOrderResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetAllByCompanyId(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAllByBranchId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAllKeetaOrdersByBranchIdQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationOrderResponse>>([]));

        var result = await _controller.GetAllByBranchId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllByBranchId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAllKeetaOrdersByBranchIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationOrderResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetAllByBranchId(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetActiveOrdersByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetActiveKeetaOrdersByBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationOrderResponse>>([]));

        var result = await _controller.GetActiveOrdersByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActiveOrdersByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetActiveKeetaOrdersByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationOrderResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetActiveOrdersByBranch(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExistsByKeetaOrderId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsKeetaOrderByKeetaOrderIdQuery>(q => q.KeetaOrderId == "ko_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsByKeetaOrderId("ko_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsByKeetaOrderId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsKeetaOrderByKeetaOrderIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsByKeetaOrderId("", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateKeetaOrderRequest ValidCreateRequest() =>
        new(1, 1, 1, 1, "ko_1", "1234", "int_1", 10, "DELIVERY", "KEETA", 50m, "{}", DateTime.UtcNow);

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var request = ValidCreateRequest();
        var response = new CreateKeetaIntegrationOrderResponse(5L, 1, 1, "ko_1", "1234", "PLACED", 50m);
        _mediator.Send(Arg.Is<CreateKeetaIntegrationOrderCommand>(c =>
                c.CompanyId == 1 && c.BranchId == 1 && c.CustomerId == 1 && c.CustomerOrderId == 1 &&
                c.KeetaMerchantId == 10 && c.OrderAmount == 50m),
            Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Create(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(5L);
        created.Value.Should().Be(response);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var request = ValidCreateRequest();
        _mediator.Send(Arg.Any<CreateKeetaIntegrationOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CreateKeetaIntegrationOrderResponse>(new Error("KeetaIntegrationOrder.AlreadyExists", "pedido ja existe")));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_MissingRequiredFields_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new CreateKeetaOrderRequest(1, 1, 1, 1, "ko_1", "1234", "int_1", 10, "DELIVERY", "KEETA", null, "{}", DateTime.UtcNow);

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateKeetaIntegrationOrderCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateKeetaOrderRequest(1, "CONFIRMED");
        _mediator.Send(Arg.Is<UpdateKeetaIntegrationOrderCommand>(c => c.Id == 1 && c.CompanyId == 1 && c.Status == "CONFIRMED"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateKeetaOrderRequest(1);
        _mediator.Send(Arg.Any<UpdateKeetaIntegrationOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_MissingCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateKeetaOrderRequest(null);

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateKeetaIntegrationOrderCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteKeetaIntegrationOrderCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteKeetaIntegrationOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Delete(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Confirm_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new ConfirmKeetaOrderRequest("motivo", 15);
        _mediator.Send(Arg.Is<ConfirmKeetaOrderCommand>(c => c.OrderId == 1 && c.Reason == "motivo" && c.PreparationTimeMinutes == 15),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Confirm(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Confirm_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new ConfirmKeetaOrderRequest();
        _mediator.Send(Arg.Any<ConfirmKeetaOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Confirm(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkReadyForPickup_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<MarkKeetaOrderReadyForPickupCommand>(c => c.OrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.MarkReadyForPickup(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkReadyForPickup_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<MarkKeetaOrderReadyForPickupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.MarkReadyForPickup(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Dispatch_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new DispatchKeetaOrderRequest("DISPATCHED", "saiu para entrega");
        _mediator.Send(Arg.Is<DispatchKeetaOrderCommand>(c => c.OrderId == 1 && c.TrackingEventType == "DISPATCHED"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Dispatch(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Dispatch_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new DispatchKeetaOrderRequest();
        _mediator.Send(Arg.Any<DispatchKeetaOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Dispatch(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkDelivered_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<MarkKeetaOrderDeliveredCommand>(c => c.OrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.MarkDelivered(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkDelivered_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<MarkKeetaOrderDeliveredCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.MarkDelivered(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SendTrackingUpdate_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new SendKeetaOrderTrackingUpdateRequest("IN_TRANSIT", "a caminho");
        _mediator.Send(Arg.Is<SendKeetaOrderTrackingUpdateCommand>(c => c.OrderId == 1 && c.TrackingEventType == "IN_TRANSIT"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SendTrackingUpdate(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SendTrackingUpdate_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SendKeetaOrderTrackingUpdateRequest("IN_TRANSIT");
        _mediator.Send(Arg.Any<SendKeetaOrderTrackingUpdateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.SendTrackingUpdate(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RequestCancellation_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new RequestKeetaOrderCancellationRequest("motivo", "code1", "FULL");
        _mediator.Send(Arg.Is<RequestKeetaOrderCancellationCommand>(c => c.OrderId == 1 && c.Reason == "motivo" && c.Mode == "FULL"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.RequestCancellation(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RequestCancellation_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new RequestKeetaOrderCancellationRequest("motivo", "code1", "FULL");
        _mediator.Send(Arg.Any<RequestKeetaOrderCancellationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.RequestCancellation(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AcceptRefund_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<AcceptKeetaOrderRefundCommand>(c => c.OrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.AcceptRefund(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AcceptRefund_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<AcceptKeetaOrderRefundCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.AcceptRefund(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RejectRefund_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new RejectKeetaOrderRefundRequest("motivo", "code1");
        _mediator.Send(Arg.Is<RejectKeetaOrderRefundCommand>(c => c.OrderId == 1 && c.Reason == "motivo" && c.Code == "code1"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.RejectRefund(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RejectRefund_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new RejectKeetaOrderRefundRequest("motivo", "code1");
        _mediator.Send(Arg.Any<RejectKeetaOrderRefundCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.RejectRefund(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
