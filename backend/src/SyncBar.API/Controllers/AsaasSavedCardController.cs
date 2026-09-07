using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Delete;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.ExistsByToken;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetById;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByToken;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/asaas/saved-cards")]
public sealed class AsaasSavedCardController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AsaasIntegrationSavedCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSavedCardController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetAsaasSavedCardByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("customer/{customerId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<AsaasIntegrationSavedCardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetByCustomerId(long customerId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSavedCardController), nameof(GetByCustomerId), async () =>
        {
            var result = await Mediator.Send(new GetSavedCardsByCustomerIdQuery(customerId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("token/{creditCardToken}")]
    [ProducesResponseType(typeof(AsaasIntegrationSavedCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByToken(string creditCardToken, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSavedCardController), nameof(GetByToken), async () =>
        {
            var result = await Mediator.Send(new GetAsaasSavedCardByTokenQuery(creditCardToken), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/token/{creditCardToken}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> ExistsByToken(string creditCardToken, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSavedCardController), nameof(ExistsByToken), async () =>
        {
            var result = await Mediator.Send(new ExistsByTokenQuery(creditCardToken), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateAsaasIntegrationSavedCardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Create(
        [FromBody] CreateAsaasSavedCardRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSavedCardController), nameof(Create), async () =>
        {
            if (request.CustomerId is null || request.CompanyId is null)
                return BadRequest(new { message = "CustomerId and CompanyId are required." });

            var command = new CreateAsaasIntegrationSavedCardCommand(
                request.CustomerId.Value,
                request.CompanyId.Value,
                request.HolderName,
                request.CardNumber,
                request.ExpiryMonth,
                request.ExpiryYear,
                request.Ccv,
                request.SetAsDefault,
                request.BranchId);

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
        [FromBody] UpdateAsaasSavedCardRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSavedCardController), nameof(Update), async () =>
        {
            if (request.CustomerId is null || request.CompanyId is null)
                return BadRequest(new { message = "CustomerId and CompanyId are required." });

            var command = new UpdateAsaasIntegrationSavedCardCommand(
                id,
                request.CustomerId.Value,
                request.CompanyId.Value,
                request.HolderName,
                request.ExpiryMonth,
                request.ExpiryYear,
                request.SetAsDefault);

            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPatch("{id:long}/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetDefault(
        long id,
        [FromBody] SetDefaultSavedCardRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSavedCardController), nameof(SetDefault), async () =>
        {
            if (request.CustomerId is null || request.CompanyId is null)
                return BadRequest(new { message = "CustomerId and CompanyId are required." });

            var command = new UpdateAsaasIntegrationSavedCardCommand(
                id,
                request.CustomerId.Value,
                request.CompanyId.Value,
                SetAsDefault: true);

            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(
        long id,
        [FromQuery] long customerId,
        [FromQuery] long companyId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSavedCardController), nameof(Delete), async () =>
        {
            var command = new DeleteAsaasIntegrationSavedCardCommand(id, customerId, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record CreateAsaasSavedCardRequest(
    long? CustomerId,
    long? CompanyId,
    string HolderName,
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string Ccv,
    bool SetAsDefault = false,
    long? BranchId = null);

public sealed record UpdateAsaasSavedCardRequest(
    long? CustomerId,
    long? CompanyId,
    string? HolderName = null,
    string? ExpiryMonth = null,
    string? ExpiryYear = null,
    bool? SetAsDefault = null);

public sealed record SetDefaultSavedCardRequest(
    long? CustomerId,
    long? CompanyId);