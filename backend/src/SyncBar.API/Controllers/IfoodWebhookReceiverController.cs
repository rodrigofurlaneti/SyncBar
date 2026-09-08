using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhook/ifood/{companyId:long}")]
public sealed class IfoodWebhookReceiverController(
    IIfoodWebhookReceiver receiver, ILogger<IfoodWebhookReceiverController> logger) : ControllerBase
{
    private const int MaximumBodyBytes = 1_048_576;

    [HttpPost]
    [RequestSizeLimit(MaximumBodyBytes)]
    public async Task<IActionResult> Receive(long companyId, CancellationToken cancellationToken,
        [FromHeader(Name = "X-IFood-Signature")] string? signature = null)
    {
        if (companyId <= 0) return BadRequest();
        if (Request.ContentLength > MaximumBodyBytes) return StatusCode(413);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await Request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (body.Length + read > MaximumBodyBytes) return StatusCode(413);
            await body.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        try
        {
            var receipt = await receiver.ReceiveAsync(companyId, body.ToArray(), signature, cancellationToken);
            return receipt.MerchantIds is null ? StatusCode(receipt.StatusCode)
                : StatusCode(receipt.StatusCode, new { merchantIds = receipt.MerchantIds });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao persistir webhook iFood para empresa {CompanyId}.", companyId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
