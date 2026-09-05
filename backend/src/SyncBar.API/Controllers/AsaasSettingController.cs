using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Asaas.Setting;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Create;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Delete;
using SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForBranch;
using SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForCompany;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchId;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyId;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetById;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/asaas/settings")]
public sealed class AsaasSettingController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AsaasIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetAsaasSettingByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}")]
    [ProducesResponseType(typeof(AsaasIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByCompanyId(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(GetByCompanyId), async () =>
        {
            var result = await Mediator.Send(new GetAsaasSettingByCompanyIdQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}/branch/{branchId:long}")]
    [ProducesResponseType(typeof(AsaasIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByBranchId(long companyId, long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(GetByBranchId), async () =>
        {
            var result = await Mediator.Send(new GetAsaasSettingByBranchIdQuery(companyId, branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("resolve")]
    [ProducesResponseType(typeof(AsaasIntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ResolveActiveSetting(
        [FromQuery] long companyId,
        [FromQuery] long? branchId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(ResolveActiveSetting), async () =>
        {
            var query = new GetByBranchOrCompanyFallbackQuery(companyId, branchId);
            var result = await Mediator.Send(query, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}/active")]
    [ProducesResponseType(typeof(IReadOnlyList<AsaasIntegrationSettingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAllActive(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(GetAllActive), async () =>
        {
            var result = await Mediator.Send(new GetAllActiveAsaasSettingsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/company/{companyId:long}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsForCompany(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(ExistsForCompany), async () =>
        {
            var result = await Mediator.Send(new ExistsAsaasSettingForCompanyQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/company/{companyId:long}/branch/{branchId:long}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsForBranch(long companyId, long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(ExistsForBranch), async () =>
        {
            var result = await Mediator.Send(new ExistsAsaasSettingForBranchQuery(companyId, branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(CreateAsaasIntegrationSettingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateAsaasIntegrationSettingCommand command,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(Create), async () =>
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
        [FromBody] UpdateAsaasSettingRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(Update), async () =>
        {
            var command = new UpdateAsaasIntegrationSettingCommand(
                id,
                request.CompanyId,
                request.ApiKey,
                request.WebhookToken,
                request.Environment,
                request.IsActive);

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
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasSettingController), nameof(Delete), async () =>
        {
            var command = new DeleteAsaasIntegrationSettingCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateAsaasSettingRequest(
    long CompanyId,
    string? ApiKey = null,
    string? WebhookToken = null,
    string? Environment = null,
    bool? IsActive = null);