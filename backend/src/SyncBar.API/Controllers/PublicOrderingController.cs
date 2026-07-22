using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.PublicOrdering.AddItem;
using SyncBar.Application.Features.PublicOrdering.GetPublicMenu;

namespace SyncBar.API.Controllers;

// Autoatendimento via QR Code — sem autenticação (o cliente final não tem login).
// O "segredo" é o token da mesa (GUID imprevisível gerado em TablesController.GenerateQrToken).
// Segue o padrão api/[controller] (= api/PublicOrdering) como os demais controllers —
// ver rate limit dedicado em Program.cs ("public-ordering").
[AllowAnonymous]
[EnableRateLimiting("public-ordering")]
public sealed class PublicOrderingController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("{token:guid}/menu")]
    public async Task<IActionResult> GetMenu(Guid token, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublicMenuQuery(token), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost("{token:guid}/items")]
    public async Task<IActionResult> AddItem(Guid token, [FromBody] AddPublicOrderItemRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new AddPublicOrderItemCommand(token, request.ProductId, request.Quantity, request.Notes), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(new { orderId = result.Value });
    }
}

public sealed record AddPublicOrderItemRequest(long ProductId, decimal Quantity, string? Notes);
