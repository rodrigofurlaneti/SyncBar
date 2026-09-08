using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Create;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Delete;
using SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForBranch;
using SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForCompany;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActiveByCompanyId;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchId;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchOrCompanyFallback;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByCompanyId;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetById;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByScope;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/branch-payment-method-settings")]
public sealed class BranchPaymentMethodSettingController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(BranchPaymentMethodSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetByIdBranchPaymentMethodSettingQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}")]
    [ProducesResponseType(typeof(BranchPaymentMethodSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByCompanyId(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(GetByCompanyId), async () =>
        {
            var result = await Mediator.Send(new GetByCompanyIdBranchPaymentMethodSettingQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}")]
    [ProducesResponseType(typeof(BranchPaymentMethodSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByBranchId(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(GetByBranchId), async () =>
        {
            var result = await Mediator.Send(new GetByBranchIdBranchPaymentMethodSettingQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("scope")]
    [ProducesResponseType(typeof(BranchPaymentMethodSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByScope(
        [FromQuery] long companyId,
        [FromQuery] long? branchId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(GetByScope), async () =>
        {
            var query = new GetByScopeBranchPaymentMethodSettingQuery(companyId, branchId);
            var result = await Mediator.Send(query, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("resolve")]
    [ProducesResponseType(typeof(BranchPaymentMethodSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ResolveActiveSetting(
        [FromQuery] long companyId,
        [FromQuery] long? branchId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(ResolveActiveSetting), async () =>
        {
            var query = new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery(companyId, branchId);
            var result = await Mediator.Send(query, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchPaymentMethodSettingResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAllActive(CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(GetAllActive), async () =>
        {
            var result = await Mediator.Send(new GetAllActiveBranchPaymentMethodSettingQuery(), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}/active")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchPaymentMethodSettingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAllActiveByCompanyId(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(GetAllActiveByCompanyId), async () =>
        {
            var result = await Mediator.Send(new GetAllActiveByCompanyIdBranchPaymentMethodSettingQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/company/{companyId:long}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsForCompany(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(ExistsForCompany), async () =>
        {
            var result = await Mediator.Send(new ExistsBranchPaymentMethodSettingForCompanyQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists/company/{companyId:long}/branch/{branchId:long}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<IActionResult> ExistsForBranch(long companyId, long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(ExistsForBranch), async () =>
        {
            var result = await Mediator.Send(new ExistsBranchPaymentMethodSettingForBranchQuery(companyId, branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost]
    [ProducesResponseType(typeof(CreateBranchPaymentMethodSettingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateBranchPaymentMethodSettingRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(Create), async () =>
        {
            if (request.CompanyId is null)
                return BadRequest(new { message = "CompanyId is required." });

            var command = new CreateBranchPaymentMethodSettingCommand(
                request.CompanyId.Value,
                request.BranchId,
                request.EnablePix,
                request.EnableBoleto,
                request.EnableCreditCard,
                request.EnableDebitCard,
                request.EnableCashMachine,
                request.IsActive);

            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(
        long id,
        [FromBody] UpdateBranchPaymentMethodSettingRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(Update), async () =>
        {
            if (request.CompanyId is null)
                return BadRequest(new { message = "CompanyId is required." });

            var command = new UpdateBranchPaymentMethodSettingCommand(
                id,
                request.CompanyId.Value,
                request.EnablePix,
                request.EnableBoleto,
                request.EnableCreditCard,
                request.EnableDebitCard,
                request.EnableCashMachine,
                request.IsActive);

            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(
        long id,
        [FromQuery] long companyId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchPaymentMethodSettingController), nameof(Delete), async () =>
        {
            var command = new DeleteBranchPaymentMethodSettingCommand(id, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record CreateBranchPaymentMethodSettingRequest(
    long? CompanyId,
    long? BranchId,
    bool EnablePix = true,
    bool EnableBoleto = true,
    bool EnableCreditCard = true,
    bool EnableDebitCard = true,
    bool EnableCashMachine = true,
    bool IsActive = true);

public sealed record UpdateBranchPaymentMethodSettingRequest(
    long? CompanyId,
    bool? EnablePix = null,
    bool? EnableBoleto = null,
    bool? EnableCreditCard = null,
    bool? EnableDebitCard = null,
    bool? EnableCashMachine = null,
    bool? IsActive = null);