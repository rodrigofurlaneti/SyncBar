using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Fiscal.IssueDocument;

namespace SyncBar.API.Controllers;

// Emissão de NFC-e. Implementação padrão é fake — troque o registro de
// IFiscalDocumentService em SyncBar.Infrastructure.DependencyInjection por um provider real
// (ex.: Focus NFe, eNotas) com certificado digital A1 antes de usar em produção.
[Authorize(Policy = "Feature:Faturamento")]
public sealed class FiscalController(IMediator mediator) : ApiController(mediator)
{
    [HttpPost("issue")]
    public async Task<IActionResult> Issue([FromBody] IssueFiscalDocumentCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
}
