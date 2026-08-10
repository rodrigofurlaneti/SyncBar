using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Comandas.GetByBranch;
using SyncBar.Application.Features.Comandas.Settings;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Salao")]
public sealed class ComandasController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComandasController), nameof(GetByBranch), async () =>
        {
            var result = await Mediator.Send(new GetComandasByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("settings/branch/{branchId:long}")]
    public Task<IActionResult> GetSettings(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComandasController), nameof(GetSettings), async () =>
        {
            var result = await Mediator.Send(new GetComandaSettingQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Limite padrao: so o gerente altera.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("settings")]
    public Task<IActionResult> SetDefaultLimit([FromBody] SetComandaDefaultLimitCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComandasController), nameof(SetDefaultLimit), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}