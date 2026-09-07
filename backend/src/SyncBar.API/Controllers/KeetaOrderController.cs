using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/keeta/orders")]
public sealed class KeetaOrderController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetKeetaOrderByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("keeta-order/{keetaOrderId}")]
    [ProducesResponseType(typeof(KeetaIntegrationOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByKeetaOrderId(string keetaOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(GetByKeetaOrderId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaOrderByKeetaOrderIdQuery(keetaOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("display/{displayId}")]
    [ProducesResponseType(typeof(KeetaIntegrationOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByDisplayId(string displayId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(GetByDisplayId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaOrderByDisplayIdQuery(displayId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationOrderResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAllByCompanyId(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(GetAllByCompanyId), async () =>
        {
            var result = await Mediator.Send(new GetAllKeetaOrdersByCompanyIdQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationOrderResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAllByBranchId(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(GetAllByBranchId), async () =>
        {
            var result = await Mediator.Send(new GetAllKeetaOrdersByBranchIdQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}/active")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationOrderResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetActiveOrdersByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(GetActiveOrdersByBranch), async () =>
        {
            var result = await Mediator.Send(new GetActiveKeetaOrdersByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/keeta-order/{keetaOrderId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsByKeetaOrderId(string keetaOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(ExistsByKeetaOrderId), async () =>
        {
            var result = await Mediator.Send(new ExistsKeetaOrderByKeetaOrderIdQuery(keetaOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateKeetaIntegrationOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateKeetaOrderRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(Create), async () =>
        {
            if (request.CompanyId is null || request.BranchId is null || request.CustomerId is null ||
                request.CustomerOrderId is null || request.KeetaMerchantId is null || request.OrderAmount is null)
                return BadRequest(new
                {
                    message = "CompanyId, BranchId, CustomerId, CustomerOrderId, KeetaMerchantId and OrderAmount are required."
                });

            var command = new CreateKeetaIntegrationOrderCommand(
                request.CompanyId.Value,
                request.BranchId.Value,
                request.CustomerId.Value,
                request.CustomerOrderId.Value,
                request.KeetaOrderId,
                request.DisplayId,
                request.InternalMerchantId,
                request.KeetaMerchantId.Value,
                request.OrderType,
                request.DeliveredBy,
                request.OrderAmount.Value,
                request.RawOrderJson,
                request.OrderCreatedAtUtc);

            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        });

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(
        long id,
        [FromBody] UpdateKeetaOrderRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(Update), async () =>
        {
            if (request.CompanyId is null)
                return BadRequest(new { message = "CompanyId is required." });

            var command = new UpdateKeetaIntegrationOrderCommand(id, request.CompanyId.Value, request.Status);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(
        long id,
        [FromQuery] long companyId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(Delete), async () =>
        {
            var command = new DeleteKeetaIntegrationOrderCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // ---- Ações do ciclo de vida do pedido na API real da Keeta ----

    [HttpPost("{id:long}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Confirm(long id, [FromBody] ConfirmKeetaOrderRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(Confirm), async () =>
        {
            var command = new ConfirmKeetaOrderCommand(id, request.Reason, request.PreparationTimeMinutes);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/ready-for-pickup")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarkReadyForPickup(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(MarkReadyForPickup), async () =>
        {
            var result = await Mediator.Send(new MarkKeetaOrderReadyForPickupCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/dispatch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Dispatch(long id, [FromBody] DispatchKeetaOrderRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(Dispatch), async () =>
        {
            var command = new DispatchKeetaOrderCommand(id, request.TrackingEventType, request.TrackingEventMessage);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/delivered")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarkDelivered(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(MarkDelivered), async () =>
        {
            var result = await Mediator.Send(new MarkKeetaOrderDeliveredCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/tracking")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SendTrackingUpdate(long id, [FromBody] SendKeetaOrderTrackingUpdateRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(SendTrackingUpdate), async () =>
        {
            var command = new SendKeetaOrderTrackingUpdateCommand(id, request.TrackingEventType, request.TrackingEventMessage);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/request-cancellation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RequestCancellation(long id, [FromBody] RequestKeetaOrderCancellationRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(RequestCancellation), async () =>
        {
            var command = new RequestKeetaOrderCancellationCommand(
                id, request.Reason, request.Code, request.Mode, request.OutOfStockItems, request.InvalidItems);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/accept-refund")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> AcceptRefund(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(AcceptRefund), async () =>
        {
            var result = await Mediator.Send(new AcceptKeetaOrderRefundCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/reject-refund")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RejectRefund(long id, [FromBody] RejectKeetaOrderRefundRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(RejectRefund), async () =>
        {
            var command = new RejectKeetaOrderRefundCommand(id, request.Reason, request.Code);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record CreateKeetaOrderRequest(
    long? CompanyId,
    long? BranchId,
    long? CustomerId,
    long? CustomerOrderId,
    string KeetaOrderId,
    string DisplayId,
    string InternalMerchantId,
    long? KeetaMerchantId,
    string OrderType,
    string DeliveredBy,
    decimal? OrderAmount,
    string RawOrderJson,
    [property: JsonRequired] DateTime OrderCreatedAtUtc);

public sealed record UpdateKeetaOrderRequest(
    long? CompanyId,
    string? Status = null);

public sealed record ConfirmKeetaOrderRequest(string? Reason = null, int? PreparationTimeMinutes = null);

public sealed record DispatchKeetaOrderRequest(string? TrackingEventType = null, string? TrackingEventMessage = null);

public sealed record SendKeetaOrderTrackingUpdateRequest(string TrackingEventType, string? TrackingEventMessage = null);

public sealed record RequestKeetaOrderCancellationRequest(
    string Reason,
    string Code,
    string Mode,
    IReadOnlyList<string>? OutOfStockItems = null,
    IReadOnlyList<string>? InvalidItems = null);

public sealed record RejectKeetaOrderRefundRequest(string Reason, string Code);
