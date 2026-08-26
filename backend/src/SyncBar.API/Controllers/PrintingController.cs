using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Printing.GetSettings;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class PrintingController(
    IMediator mediator,
    IPrintingService printingService,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("settings/branch/{branchId:long}")]
    public Task<IActionResult> GetSettings(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PrintingController), nameof(GetSettings), async () =>
        {
            var result = await Mediator.Send(new GetPrintSettingsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("bill/{orderId:long}")]
    public Task<IActionResult> PrintBill(long orderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PrintingController), nameof(PrintBill), async () =>
        {
            var result = await printingService.PrintBillAsync(orderId, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("receipt/{saleId:long}")]
    public Task<IActionResult> PrintReceipt(long saleId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PrintingController), nameof(PrintReceipt), async () =>
        {
            var result = await printingService.PrintPaymentReceiptAsync(saleId, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("partial-receipt/{partialId:long}")]
    public Task<IActionResult> PrintPartialReceipt(long partialId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PrintingController), nameof(PrintPartialReceipt), async () =>
        {
            var result = await printingService.PrintPartialReceiptAsync(partialId, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("cash-session/{sessionId:long}")]
    public Task<IActionResult> PrintCashClosing(long sessionId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PrintingController), nameof(PrintCashClosing), async () =>
        {
            var result = await printingService.PrintCashClosingAsync(sessionId, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}