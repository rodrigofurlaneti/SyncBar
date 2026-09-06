using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SyncBar.Tests.API.Controllers.TestSupport;

// Todo controller do projeto herda de ApiController, cujo ExecuteWithLogAsync acessa
// HttpContext.User/Connection/RequestServices — sem isso, qualquer teste de ação de controller
// lança NullReferenceException antes mesmo de chegar na lógica que se quer testar.
internal static class ControllerTestHelpers
{
    public static void AttachHttpContext(ControllerBase controller, IServiceProvider? services = null, string? userId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (services is not null)
            httpContext.RequestServices = services;
        if (userId is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }
}
