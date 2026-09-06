using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Create;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Delete;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByAfterSaleOrderId;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByOrderId;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetPendingDisputesByBranch;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/keeta/refund-disputes")]
public sealed class KeetaRefundDisputeController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationRefundDisputeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaRefundDisputeController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetKeetaRefundDisputeByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("after-sale-order/{afterSaleOrderId:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationRefundDisputeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByAfterSaleOrderId(long afterSaleOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaRefundDisputeController), nameof(GetByAfterSaleOrderId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaRefundDisputeByAfterSaleOrderIdQuery(afterSaleOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("order/{orderId}")]
    [ProducesResponseType(typeof(KeetaIntegrationRefundDisputeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByOrderId(string orderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaRefundDisputeController), nameof(GetByOrderId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaRefundDisputeByOrderIdQuery(orderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationRefundDisputeResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetPendingDisputesByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaRefundDisputeController), nameof(GetPendingDisputesByBranch), async () =>
        {
            var result = await Mediator.Send(new GetPendingKeetaRefundDisputesByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/after-sale-order/{afterSaleOrderId:long}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsByAfterSaleOrderId(long afterSaleOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaRefundDisputeController), nameof(ExistsByAfterSaleOrderId), async () =>
        {
            var result = await Mediator.Send(new ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery(afterSaleOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateKeetaIntegrationRefundDisputeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateKeetaIntegrationRefundDisputeCommand command,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaRefundDisputeController), nameof(Create), async () =>
        {
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
        [FromBody] UpdateKeetaRefundDisputeRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaRefundDisputeController), nameof(Update), async () =>
        {
            var command = new UpdateKeetaIntegrationRefundDisputeCommand(
                id, request.CompanyId, request.Accepted, request.DenialReasonCode, request.DenialReasonText);
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
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaRefundDisputeController), nameof(Delete), async () =>
        {
            var command = new DeleteKeetaIntegrationRefundDisputeCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateKeetaRefundDisputeRequest(
    long CompanyId,
    bool Accepted,
    string? DenialReasonCode = null,
    string? DenialReasonText = null);
