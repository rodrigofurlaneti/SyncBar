using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Create;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Delete;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetAllByOrderId;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetByEventId;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetUnprocessedEvents;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/keeta/order-events")]
public sealed class KeetaOrderEventLogController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationOrderEventLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderEventLogController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetKeetaOrderEventLogByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("event/{eventId}")]
    [ProducesResponseType(typeof(KeetaIntegrationOrderEventLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByEventId(string eventId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderEventLogController), nameof(GetByEventId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaOrderEventLogByEventIdQuery(eventId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("order/{orderId}")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationOrderEventLogResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAllByOrderId(string orderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderEventLogController), nameof(GetAllByOrderId), async () =>
        {
            var result = await Mediator.Send(new GetAllKeetaOrderEventLogsByOrderIdQuery(orderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("unprocessed")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationOrderEventLogResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetUnprocessed(CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderEventLogController), nameof(GetUnprocessed), async () =>
        {
            var result = await Mediator.Send(new GetUnprocessedKeetaOrderEventLogsQuery(), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/event/{eventId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsByEventId(string eventId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderEventLogController), nameof(ExistsByEventId), async () =>
        {
            var result = await Mediator.Send(new ExistsKeetaOrderEventLogByEventIdQuery(eventId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateKeetaIntegrationOrderEventLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateKeetaIntegrationOrderEventLogCommand command,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderEventLogController), nameof(Create), async () =>
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
        [FromBody] UpdateKeetaOrderEventLogRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderEventLogController), nameof(Update), async () =>
        {
            var command = new UpdateKeetaIntegrationOrderEventLogCommand(
                id, request.CompanyId, request.MarkAsProcessed, request.ErrorMessage);
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
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderEventLogController), nameof(Delete), async () =>
        {
            var command = new DeleteKeetaIntegrationOrderEventLogCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateKeetaOrderEventLogRequest(
    long CompanyId,
    bool MarkAsProcessed = false,
    string? ErrorMessage = null);
