using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Delete;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetById;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Update;
using SyncBar.Domain.Enums;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/asaas/webhook-logs")]
public sealed class AsaasWebhookLogController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AsaasWebhookLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(
        long id,
        [FromQuery] long companyId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasWebhookLogController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetAsaasWebhookLogByIdQuery(id, companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("payment/{paymentId}")]
    [ProducesResponseType(typeof(IReadOnlyList<AsaasWebhookLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetByPaymentId(
        string paymentId,
        [FromQuery] long companyId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasWebhookLogController), nameof(GetByPaymentId), async () =>
        {
            var result = await Mediator.Send(new GetAsaasWebhookLogsByPaymentIdQuery(companyId, paymentId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("unprocessed")]
    [ProducesResponseType(typeof(IReadOnlyList<AsaasWebhookLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetUnprocessed(
        [FromQuery] long companyId,
        CancellationToken ct,
        [FromQuery] int limit = 50) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasWebhookLogController), nameof(GetUnprocessed), async () =>
        {
            var result = await Mediator.Send(new GetUnprocessedAsaasWebhookLogsQuery(companyId, limit), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("events/{asaasEventId}/processed")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> HasAlreadyProcessedEvent(
        string asaasEventId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasWebhookLogController), nameof(HasAlreadyProcessedEvent), async () =>
        {
            var result = await Mediator.Send(new HasAlreadyProcessedEventQuery(asaasEventId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateWebhookLogStatusRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasWebhookLogController), nameof(UpdateStatus), async () =>
        {
            var command = new UpdateAsaasWebhookLogStatusCommand(
                id,
                request.CompanyId,
                request.Status,
                request.ErrorMessage);

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
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasWebhookLogController), nameof(Delete), async () =>
        {
            var command = new DeleteAsaasWebhookLogCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateWebhookLogStatusRequest(
    long CompanyId,
    WebhookLogStatus Status,
    string? ErrorMessage = null);