using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Tables.GenerateQrToken;
using SyncBar.Application.Features.Tables.GetByBranch;
using SyncBar.Application.Features.Tables.SetReadingValidation;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class TablesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(TablesController), nameof(GetByBranch), async () =>
        {
            var result = await Mediator.Send(new GetTablesByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost("{id:long}/qr-token")]
    public Task<IActionResult> GenerateQrToken(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(TablesController), nameof(GenerateQrToken), async () =>
        {
            var result = await Mediator.Send(new GenerateTableQrTokenCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { token = result.Value });
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/reading-validation")]
    public Task<IActionResult> SetReadingValidation(long id, [FromBody] SetReadingValidationRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(TablesController), nameof(SetReadingValidation), async () =>
        {
            var command = new SetDiningTableReadingValidationCommand(
                id, request.IsCameraInputEnabled, request.IsBarcodeEnabled, request.IsQrCodeEnabled);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}
public sealed record SetReadingValidationRequest(bool IsCameraInputEnabled, bool IsBarcodeEnabled, bool IsQrCodeEnabled);