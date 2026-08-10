using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Suppliers.Create;
using SyncBar.Application.Features.Suppliers.Deactivate;
using SyncBar.Application.Features.Suppliers.GetByCompany;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Estoque")]
public sealed class SuppliersController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("company/{companyId:long}")]
    public Task<IActionResult> GetByCompany(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SuppliersController), nameof(GetByCompany), async () =>
        {
            var result = await Mediator.Send(new GetSuppliersByCompanyQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateSupplierCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SuppliersController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}/deactivate")]
    public Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SuppliersController), nameof(Deactivate), async () =>
        {
            var result = await Mediator.Send(new DeactivateSupplierCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}