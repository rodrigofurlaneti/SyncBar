using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.PublicOrdering.AddItem;
using SyncBar.Application.Features.PublicOrdering.GetPublicMenu;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[AllowAnonymous]
[EnableRateLimiting("public-ordering")]
public sealed class PublicOrderingController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{token:guid}/menu")]
    public Task<IActionResult> GetMenu(Guid token, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PublicOrderingController), nameof(GetMenu), async () =>
        {
            var result = await Mediator.Send(new GetPublicMenuQuery(token), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("{token:guid}/items")]
    public Task<IActionResult> AddItem(Guid token, [FromBody] AddPublicOrderItemRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PublicOrderingController), nameof(AddItem), async () =>
        {
            var result = await Mediator.Send(new AddPublicOrderItemCommand(
                token, request.ProductId, request.Quantity, request.Notes, request.Complements), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { orderId = result.Value });
        });
}

public sealed record AddPublicOrderItemRequest(
    [property: JsonRequired] long ProductId,
    [property: JsonRequired] decimal Quantity,
    string? Notes,
    IReadOnlyCollection<OrderItemComplementSelection>? Complements = null);
