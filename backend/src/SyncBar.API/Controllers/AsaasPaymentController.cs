using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Delete;
using SyncBar.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByBranchId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetById;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetPendingByBranchId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/asaas/payments")]
public sealed class AsaasPaymentController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AsaasIntegrationPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetAsaasPaymentByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("asaas-id/{asaasPaymentId}")]
    [ProducesResponseType(typeof(AsaasIntegrationPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByAsaasPaymentId(string asaasPaymentId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(GetByAsaasPaymentId), async () =>
        {
            var result = await Mediator.Send(new GetByAsaasPaymentIdQuery(asaasPaymentId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("order/{customerOrderId:long}")]
    [ProducesResponseType(typeof(AsaasIntegrationPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByCustomerOrderId(long customerOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(GetByCustomerOrderId), async () =>
        {
            var result = await Mediator.Send(new GetByCustomerOrderIdQuery(customerOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<AsaasIntegrationPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetByBranchId(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(GetByBranchId), async () =>
        {
            var result = await Mediator.Send(new GetByBranchIdQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<AsaasIntegrationPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetPendingByBranchId(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(GetPendingByBranchId), async () =>
        {
            var result = await Mediator.Send(new GetPendingAsaasPaymentsByBranchIdQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/{asaasPaymentId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Exists(string asaasPaymentId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(Exists), async () =>
        {
            var result = await Mediator.Send(new ExistsByAsaasPaymentIdQuery(asaasPaymentId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateAsaasIntegrationPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create(
        [FromBody] CreateAsaasIntegrationPaymentCommand command,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetById), new { id = result.Value.PaymentId }, result.Value);
        });

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(
        long id,
        [FromBody] UpdateAsaasPaymentRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(Update), async () =>
        {
            var command = new UpdateAsaasIntegrationPaymentCommand(
                id,
                request.Status,
                request.NetValue,
                request.PaymentDate,
                request.PixQrCodeBase64,
                request.PixPayload,
                request.InvoiceUrl,
                request.BankSlipUrl);

            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasPaymentController), nameof(Delete), async () =>
        {
            var command = new DeleteAsaasPaymentCommand(id);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateAsaasPaymentRequest(
    string Status,
    decimal? NetValue = null,
    DateTime? PaymentDate = null,
    string? PixQrCodeBase64 = null,
    string? PixPayload = null,
    string? InvoiceUrl = null,
    string? BankSlipUrl = null);