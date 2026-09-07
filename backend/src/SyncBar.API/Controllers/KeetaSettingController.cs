using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Create;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Delete;
using SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForBranch;
using SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForCompany;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchId;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchOrCompanyFallback;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByCompanyId;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByScope;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/keeta/settings")]
public sealed class KeetaSettingController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetKeetaSettingByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByCompanyId(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(GetByCompanyId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaSettingByCompanyIdQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}")]
    [ProducesResponseType(typeof(KeetaIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByBranchId(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(GetByBranchId), async () =>
        {
            var result = await Mediator.Send(new GetKeetaSettingByBranchIdQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("resolve")]
    [ProducesResponseType(typeof(KeetaIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ResolveActiveSetting(
        [FromQuery] long companyId,
        [FromQuery] long? branchId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(ResolveActiveSetting), async () =>
        {
            var query = new GetKeetaSettingByBranchOrCompanyFallbackQuery(companyId, branchId);
            var result = await Mediator.Send(query, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("scope")]
    [ProducesResponseType(typeof(KeetaIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByScope(
        [FromQuery] long companyId,
        [FromQuery] long? branchId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(GetByScope), async () =>
        {
            var result = await Mediator.Send(new GetKeetaSettingByScopeQuery(companyId, branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/company/{companyId:long}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsForCompany(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(ExistsForCompany), async () =>
        {
            var result = await Mediator.Send(new ExistsKeetaSettingForCompanyQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/branch/{branchId:long}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsForBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(ExistsForBranch), async () =>
        {
            var result = await Mediator.Send(new ExistsKeetaSettingForBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateKeetaIntegrationSettingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateKeetaSettingRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(Create), async () =>
        {
            if (request.CompanyId is null || request.BranchId is null)
                return BadRequest(new { message = "CompanyId and BranchId are required." });

            var command = new CreateKeetaIntegrationSettingCommand(
                request.CompanyId.Value,
                request.BranchId.Value,
                request.ClientId,
                request.ClientSecret,
                request.AppId,
                request.BaseUrl);

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
        [FromBody] UpdateKeetaSettingRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(Update), async () =>
        {
            if (request.CompanyId is null)
                return BadRequest(new { message = "CompanyId is required." });

            var command = new UpdateKeetaIntegrationSettingCommand(
                id,
                request.CompanyId.Value,
                request.ClientId,
                request.ClientSecret,
                request.AppId,
                request.BaseUrl);

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
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaSettingController), nameof(Delete), async () =>
        {
            var command = new DeleteKeetaIntegrationSettingCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record CreateKeetaSettingRequest(
    long? CompanyId,
    long? BranchId,
    string ClientId,
    string ClientSecret,
    string AppId,
    string? BaseUrl = null);

public sealed record UpdateKeetaSettingRequest(
    long? CompanyId,
    string? ClientId = null,
    string? ClientSecret = null,
    string? AppId = null,
    string? BaseUrl = null);
