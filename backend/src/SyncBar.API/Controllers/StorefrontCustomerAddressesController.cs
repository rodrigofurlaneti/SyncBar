using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.CustomerAddresses.Create;
using SyncBar.Application.Features.CustomerAddresses.GetByCustomerId;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Roles = "Customer")]
[Route("api/storefront/customer/addresses")]
public sealed class StorefrontCustomerAddressesController(IMediator mediator, IBranchRepository branches) : ApiController(mediator)
{
    private bool Identity(out long customerId, out long companyId)
    {
        companyId = 0;
        return long.TryParse(User.FindFirstValue("customerId"), out customerId) && customerId > 0
            && long.TryParse(User.FindFirstValue("companyId"), out companyId) && companyId > 0;
    }

    [HttpGet("customer/{customerId:long}")]
    public async Task<IActionResult> Get(long customerId, CancellationToken ct)
    {
        if (!Identity(out var ownerId, out _) || ownerId != customerId) return Forbid();
        var result = await Mediator.Send(new GetCustomerAddressesByCustomerIdQuery(ownerId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StorefrontAddressRequest request, CancellationToken ct)
    {
        if (!Identity(out var customerId, out var companyId)) return Forbid();
        var branch = await branches.GetByIdAsync(request.BranchId, ct);
        if (branch is null || !branch.IsActive || branch.CompanyId != companyId) return Forbid();
        var result = await Mediator.Send(new CreateCustomerAddressCommand(companyId, request.BranchId, customerId,
            request.Street, request.Number, request.Supplement ?? "", request.ZipCode), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(new { id = result.Value });
    }
}

public sealed record StorefrontAddressRequest(long BranchId, string Street, string Number, string? Supplement, string ZipCode);
