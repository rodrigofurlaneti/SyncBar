using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Access.GetFeatures;
using SyncBar.Application.Features.Access.GetJobTitleFeatures;
using SyncBar.Application.Features.Access.GetMyFeatures;
using SyncBar.Application.Features.Access.GetUserFeatures;
using SyncBar.Application.Features.Access.SetJobTitleFeatures;
using SyncBar.Application.Features.Access.SetUserFeatures;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class AccessController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    private const string ManagerRoles = "Administrador,Gerente";

    // Qualquer usuario autenticado consulta as proprias telas.
    [HttpGet("my-features")]
    public Task<IActionResult> GetMyFeatures(CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetMyFeatures), async () =>
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!long.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var isManager = FeatureCodes.ManagerRoles.Any(User.IsInRole);
            var result = await Mediator.Send(new GetMyFeaturesQuery(userId, isManager), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Gestao de acessos: somente Gerente/Administrador.
    [Authorize(Roles = ManagerRoles)]
    [HttpGet("features")]
    public Task<IActionResult> GetFeatures(CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetFeatures), async () =>
        {
            var result = await Mediator.Send(new GetFeaturesQuery(), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpGet("jobtitles/{jobTitleId:long}/features")]
    public Task<IActionResult> GetJobTitleFeatures(long jobTitleId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetJobTitleFeatures), async () =>
        {
            var result = await Mediator.Send(new GetJobTitleFeaturesQuery(jobTitleId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("jobtitles/{jobTitleId:long}/features")]
    public Task<IActionResult> SetJobTitleFeatures(long jobTitleId, [FromBody] SetFeaturesRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(SetJobTitleFeatures), async () =>
        {
            var result = await Mediator.Send(new SetJobTitleFeaturesCommand(jobTitleId, request.FeatureIds), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpGet("users/{appUserId:long}/features")]
    public Task<IActionResult> GetUserFeatures(long appUserId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetUserFeatures), async () =>
        {
            var result = await Mediator.Send(new GetUserFeaturesQuery(appUserId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("users/{appUserId:long}/features")]
    public Task<IActionResult> SetUserFeatures(long appUserId, [FromBody] SetFeaturesRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(SetUserFeatures), async () =>
        {
            var result = await Mediator.Send(new SetUserFeaturesCommand(appUserId, request.FeatureIds), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // --- WRAPPER DE LOG ---
    private async Task<IActionResult> ExecuteWithLogAsync(string methodName, Func<Task<IActionResult>> action)
    {
        var stopwatch = Stopwatch.StartNew();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        long? appUserId = long.TryParse(userIdClaim, out var id) ? id : null;

        var log = new LogTracker(0)
        {
            AppUserId = appUserId,
            DirectoryName = "Controllers",
            ClassName = nameof(AccessController),
            MethodName = methodName,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        try
        {
            var result = await action();

            if (result is OkObjectResult or NoContentResult or CreatedAtActionResult)
            {
                log.IsSuccess = true;
                log.Message = "Executado com sucesso.";
            }
            else
            {
                log.IsSuccess = false;
                log.Message = "Falha na regra de negócio.";

                if (result is ObjectResult objResult && objResult.Value != null)
                {
                    var valueType = objResult.Value.GetType();
                    var detailProp = valueType.GetProperty("Detail") ?? valueType.GetProperty("detail");
                    var titleProp = valueType.GetProperty("Title") ?? valueType.GetProperty("title");

                    var detailValue = detailProp?.GetValue(objResult.Value)?.ToString();
                    var titleValue = titleProp?.GetValue(objResult.Value)?.ToString();

                    log.ErrorMessage = !string.IsNullOrEmpty(detailValue)
                        ? $"{titleValue}: {detailValue}"
                        : (titleValue ?? objResult.Value.ToString());
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            log.IsSuccess = false;
            log.Message = "Erro interno no servidor.";
            log.ErrorMessage = ex.Message;
            log.StackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            log.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            try
            {
                await logRepository.AddAsync(log);
                await unitOfWork.CommitAsync();
            }
            catch
            {
                // Evita que falhas na auditoria quebrem o fluxo principal da resposta HTTP
            }
        }
    }
}

public sealed record SetFeaturesRequest(IReadOnlyCollection<long> FeatureIds);