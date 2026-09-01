using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Purchases.GetByBranch;
using SyncBar.Application.Features.Purchases.Register;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class PurchasesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PurchasesController), nameof(GetByBranch), async () =>
        {
            var result = await Mediator.Send(new GetPurchasesByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    public Task<IActionResult> Register([FromBody] RegisterPurchaseCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PurchasesController), nameof(Register), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}