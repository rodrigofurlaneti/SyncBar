using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Printing.CreatePrinter;
using SyncBar.Application.Features.Printing.DeactivatePrinter;
using SyncBar.Application.Features.Printing.GetPrinters;
using SyncBar.Application.Features.Printing.SetSettings;

namespace SyncBar.API.Controllers;

// Gestao de impressoras e interruptores — tela Impressao.
[Authorize(Policy = "Feature:Impressao")]
public sealed class PrintersController(IMediator mediator, IPrintingService printingService) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public async Task<IActionResult> GetByBranch(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPrintersQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrinterCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPut("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeactivatePrinterCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPut("settings")]
    public async Task<IActionResult> SetSettings([FromBody] SetPrintSettingsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPost("{id:long}/test")]
    public async Task<IActionResult> Test(long id, CancellationToken ct)
    {
        var result = await printingService.PrintTestAsync(id, ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}
