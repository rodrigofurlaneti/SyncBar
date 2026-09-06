using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Create;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Delete;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetByAuthId;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetPendingSessions;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/keeta/authorization-sessions")]
public sealed class KeetaAuthorizationSessionController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationAuthorizationSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaAuthorizationSessionController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetKeetaAuthorizationSessionByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("auth/{authId}")]
    [ProducesResponseType(typeof(KeetaIntegrationAuthorizationSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByAuthId(string authId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaAuthorizationSessionController), nameof(GetByAuthId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaAuthorizationSessionByAuthIdQuery(authId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationAuthorizationSessionResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetPendingSessions(CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaAuthorizationSessionController), nameof(GetPendingSessions), async () =>
        {
            var result = await Mediator.Send(new GetPendingKeetaAuthorizationSessionsQuery(), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateKeetaIntegrationAuthorizationSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateKeetaIntegrationAuthorizationSessionCommand command,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaAuthorizationSessionController), nameof(Create), async () =>
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
        [FromBody] UpdateKeetaAuthorizationSessionRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaAuthorizationSessionController), nameof(Update), async () =>
        {
            var command = new UpdateKeetaIntegrationAuthorizationSessionCommand(
                id,
                request.CompanyId,
                request.KeetaMerchantId,
                request.AuthorizationCode,
                request.State,
                request.MarkAsProcessed);

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
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaAuthorizationSessionController), nameof(Delete), async () =>
        {
            var command = new DeleteKeetaIntegrationAuthorizationSessionCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateKeetaAuthorizationSessionRequest(
    long CompanyId,
    long? KeetaMerchantId = null,
    string? AuthorizationCode = null,
    string? State = null,
    bool MarkAsProcessed = false);
