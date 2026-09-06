using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.RefreshAccessToken;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/keeta/oauth")]
public sealed class KeetaOAuthController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("authorization-url")]
    [ProducesResponseType(typeof(RequestKeetaAuthorizationUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAuthorizationUrl(
        [FromQuery] long companyId,
        [FromQuery] long branchId,
        [FromQuery] string redirectUri,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOAuthController), nameof(GetAuthorizationUrl), async () =>
        {
            var result = await Mediator.Send(new RequestKeetaAuthorizationUrlCommand(companyId, branchId, redirectUri), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Endpoint que recebe o redirecionamento do navegador do lojista ao final da autorização na
    // Keeta. O companyId/branchId chegam aqui porque foram embutidos no próprio redirectUri
    // enviado em GetAuthorizationUrl — é o único jeito de correlacionar essa autorização com o
    // tenant que a iniciou (a Keeta não aceita nenhum parâmetro de correlação próprio nesse fluxo).
    // Como quem "bate" aqui é o navegador do lojista (não uma chamada de API do frontend),
    // devolve um redirect pra tela de integrações em vez de JSON cru.
    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] long companyId,
        [FromQuery] long branchId,
        [FromQuery] string authId,
        [FromQuery] string? state,
        [FromQuery] long? keetaMerchantId,
        [FromQuery] string? code,
        CancellationToken ct)
    {
        var logRepo = HttpContext.RequestServices.GetRequiredService<ILogTrackerRepository>();
        var uow = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();

        IActionResult? handled = null;
        await ExecuteWithLogAsync(logRepo, uow, nameof(KeetaOAuthController), nameof(Callback), async () =>
        {
            var command = new HandleKeetaOAuthCallbackCommand(companyId, branchId, authId, state, keetaMerchantId, code);
            var result = await Mediator.Send(command, ct);
            handled = result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            return handled;
        });

        var succeeded = handled is OkObjectResult;
        return Redirect($"/integracoes/keeta?keetaAuth={(succeeded ? "success" : "error")}");
    }

    [HttpPost("token/refresh")]
    [ProducesResponseType(typeof(RefreshKeetaAccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> RefreshToken(
        [FromBody] RefreshKeetaAccessTokenRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(KeetaOAuthController), nameof(RefreshToken), async () =>
        {
            var command = new RefreshKeetaAccessTokenCommand(request.CompanyId, request.BranchId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}

public sealed record RefreshKeetaAccessTokenRequest(long CompanyId, long BranchId);
