using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.PublicOrdering.AddItem;
using SyncBar.Application.Features.PublicOrdering.GetPublicBill;
using SyncBar.Application.Features.PublicOrdering.GetPublicComandaBill; // 1. Adicionado o using da nova feature
using SyncBar.Application.Features.PublicOrdering.GetPublicMenu;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json.Serialization;

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

    [HttpGet("{token:guid}/bill")]
    public Task<IActionResult> GetBill(Guid token, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PublicOrderingController), nameof(GetBill), async () =>
        {
            var result = await Mediator.Send(new GetPublicBillQuery(token), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("{token:guid}/comandas/{code}/bill")]
    public Task<IActionResult> GetComandaBill(Guid token, string code, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PublicOrderingController), nameof(GetComandaBill), async () =>
        {
            var result = await Mediator.Send(new GetPublicComandaBillQuery(token, code), ct);
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