using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Printing.GetSettings;

namespace SyncBar.API.Controllers;

// Acoes de impressao usadas na operacao (salao/caixa) — qualquer usuario autenticado.
[Authorize]
public sealed class PrintingController(IMediator mediator, IPrintingService printingService) : ApiController(mediator)
{
    // O frontend consulta para decidir se mostra o "Deseja imprimir?".
    [HttpGet("settings/branch/{branchId:long}")]
    public async Task<IActionResult> GetSettings(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPrintSettingsQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost("bill/{orderId:long}")]
    public async Task<IActionResult> PrintBill(long orderId, CancellationToken ct)
    {
        var result = await printingService.PrintBillAsync(orderId, ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPost("cash-session/{sessionId:long}")]
    public async Task<IActionResult> PrintCashClosing(long sessionId, CancellationToken ct)
    {
        var result = await printingService.PrintCashClosingAsync(sessionId, ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}
