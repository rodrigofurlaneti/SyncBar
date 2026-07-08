using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Domain.Primitives;

namespace SyncBar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator Mediator = mediator;

    protected IActionResult HandleFailure(Result result)
        => result.Error.Code switch
        {
            var c when c.EndsWith(".NotFound")      => NotFound(CreateProblemDetails(result)),
            var c when c.EndsWith(".AlreadyExists") => Conflict(CreateProblemDetails(result)),
            var c when c.EndsWith(".Duplicate")     => Conflict(CreateProblemDetails(result)),
            _                                       => BadRequest(CreateProblemDetails(result))
        };

    private static ProblemDetails CreateProblemDetails(Result result)
        => new() { Title = result.Error.Code, Detail = result.Error.Message };
}
