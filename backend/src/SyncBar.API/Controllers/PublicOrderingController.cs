using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.PublicOrdering.AddItem;
using SyncBar.Application.Features.PublicOrdering.GetPublicBill;
using SyncBar.Application.Features.PublicOrdering.GetPublicComandaBill; // 1. Adicionado o using da nova feature
using SyncBar.Application.Features.PublicOrdering.GetPublicMenu;
using SyncBar.Application.Features.PublicOrdering.ValidateComandaReading;
using SyncBar.Application.Features.PublicOrdering.ValidateTableReading;
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
                token, request.ProductId, request.Quantity, request.Notes, request.Complements, request.ComandaCode), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { orderId = result.Value });
        });

    // Comprovação de leitura da comanda (câmera/código de barras/QR Code) — exigida antes de
    // consultar ou abrir pedido numa comanda, conforme os flags ligados em DiningTable.
    [HttpPost("{token:guid}/comandas/{code}/reading-validation")]
    public Task<IActionResult> ValidateComandaReading(Guid token, string code, [FromBody] ValidateComandaReadingRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PublicOrderingController), nameof(ValidateComandaReading), async () =>
        {
            var result = await Mediator.Send(new ValidateComandaReadingCommand(
                token, code, request.Method, request.ScannedValue, request.PhotoBase64), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Comprovação de leitura da MESA (câmera/código de barras/QR Code) — exigida antes de
    // liberar qualquer pedido direto na mesa quando a "Visualização do Cliente (QR Code)"
    // está desligada (sem fluxo de comanda pro cliente). A mesa já é identificada pelo
    // token — não tem código de comanda envolvido aqui.
    [HttpPost("{token:guid}/reading-validation")]
    public Task<IActionResult> ValidateTableReading(Guid token, [FromBody] ValidateComandaReadingRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PublicOrderingController), nameof(ValidateTableReading), async () =>
        {
            var result = await Mediator.Send(new ValidateTableReadingCommand(
                token, request.Method, request.ScannedValue, request.PhotoBase64), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record AddPublicOrderItemRequest(
    [property: JsonRequired] long ProductId,
    [property: JsonRequired] decimal Quantity,
    string? Notes,
    IReadOnlyCollection<OrderItemComplementSelection>? Complements = null,
    // Quando informado, o pedido vai pra conta da COMANDA (não da mesa) — ver
    // AddPublicOrderItemCommand para o porquê.
    string? ComandaCode = null);

public sealed record ValidateComandaReadingRequest(
    [property: JsonRequired] string Method,
    string? ScannedValue,
    string? PhotoBase64);