using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Fiscal.IssueDocument;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class FiscalController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [Authorize(Roles = ManagerRoles)]
    [HttpPost("issue")]
    public Task<IActionResult> Issue([FromBody] IssueFiscalDocumentCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(FiscalController), nameof(Issue), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}