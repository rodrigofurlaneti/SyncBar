using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Create;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Delete;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.ExistsByKeetaMerchantId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByBranchId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByCompanyId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByInternalMerchantId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByKeetaMerchantId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/keeta/merchant-mappings")]
public sealed class KeetaMerchantMappingController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationMerchantMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetKeetaMerchantMappingByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("keeta-merchant/{keetaMerchantId:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationMerchantMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByKeetaMerchantId(long keetaMerchantId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(GetByKeetaMerchantId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaMerchantMappingByKeetaMerchantIdQuery(keetaMerchantId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("internal-merchant/{internalMerchantId}")]
    [ProducesResponseType(typeof(KeetaIntegrationMerchantMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByInternalMerchantId(string internalMerchantId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(GetByInternalMerchantId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaMerchantMappingByInternalMerchantIdQuery(internalMerchantId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationMerchantMappingResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAllByCompanyId(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(GetAllByCompanyId), async () =>
        {
            var result = await Mediator.Send(new GetAllKeetaMerchantMappingsByCompanyIdQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<KeetaIntegrationMerchantMappingResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAllByBranchId(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(GetAllByBranchId), async () =>
        {
            var result = await Mediator.Send(new GetAllKeetaMerchantMappingsByBranchIdQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/keeta-merchant/{keetaMerchantId:long}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsByKeetaMerchantId(long keetaMerchantId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(ExistsByKeetaMerchantId), async () =>
        {
            var result = await Mediator.Send(new ExistsKeetaMerchantMappingByKeetaMerchantIdQuery(keetaMerchantId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateKeetaIntegrationMerchantMappingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateKeetaMerchantMappingRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(Create), async () =>
        {
            if (request.CompanyId is null || request.BranchId is null || request.KeetaMerchantId is null)
                return BadRequest(new { message = "CompanyId, BranchId and KeetaMerchantId are required." });

            var command = new CreateKeetaIntegrationMerchantMappingCommand(
                request.CompanyId.Value,
                request.BranchId.Value,
                request.InternalMerchantId,
                request.KeetaMerchantId.Value,
                request.StoreName);

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
        [FromBody] UpdateKeetaMerchantMappingRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(Update), async () =>
        {
            if (request.CompanyId is null)
                return BadRequest(new { message = "CompanyId is required." });

            var command = new UpdateKeetaIntegrationMerchantMappingCommand(
                id,
                request.CompanyId.Value,
                request.IsAuthorized,
                request.IsOnboarded,
                request.MenuBaseUrl,
                request.WebhookUrl,
                request.RegisterMenuSync);

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
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaMerchantMappingController), nameof(Delete), async () =>
        {
            var command = new DeleteKeetaIntegrationMerchantMappingCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record CreateKeetaMerchantMappingRequest(
    long? CompanyId,
    long? BranchId,
    string InternalMerchantId,
    long? KeetaMerchantId,
    string StoreName);

public sealed record UpdateKeetaMerchantMappingRequest(
    long? CompanyId,
    bool? IsAuthorized = null,
    bool? IsOnboarded = null,
    string? MenuBaseUrl = null,
    string? WebhookUrl = null,
    bool RegisterMenuSync = false);
