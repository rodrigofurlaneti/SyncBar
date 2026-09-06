using MediatR;
using Microsoft.AspNetCore.Mvc;
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
        [FromBody] CreateKeetaIntegrationOrderCommand command,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(Create), async () =>
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
        [FromBody] UpdateKeetaOrderRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOrderController), nameof(Update), async () =>
        {
            var command = new UpdateKeetaIntegrationOrderCommand(id, request.CompanyId, request.Status);
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
}

public sealed record UpdateKeetaOrderRequest(
    long CompanyId,
    string? Status = null);
