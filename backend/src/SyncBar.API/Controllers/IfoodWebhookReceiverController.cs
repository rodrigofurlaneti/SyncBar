using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhook/ifood/{companyId:long?}")]
public sealed class IfoodWebhookReceiverController(
    IIfoodWebhookReceiver receiver, ILogger<IfoodWebhookReceiverController> logger) : ControllerBase
{
    private const int MaximumBodyBytes = 1_048_576;

    [HttpPost]
    [RequestSizeLimit(MaximumBodyBytes)]
    public async Task<IActionResult> Receive(long? companyId, CancellationToken cancellationToken,
        [FromHeader(Name = "X-IFood-Signature")] string? signature = null)
    {
        if (companyId <= 0) return WebhookError(400);
        if (Request.ContentLength > MaximumBodyBytes) return WebhookError(413);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await Request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (body.Length + read > MaximumBodyBytes) return WebhookError(413);
            await body.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        try
        {
            var receipt = companyId.HasValue
                ? await receiver.ReceiveAsync(companyId.Value, body.ToArray(), signature, cancellationToken)
                : await receiver.ReceiveAsync(body.ToArray(), signature, cancellationToken);
            if (receipt.StatusCode >= 400) return WebhookError(receipt.StatusCode);
            return receipt.MerchantIds is null ? StatusCode(receipt.StatusCode)
                : StatusCode(receipt.StatusCode, new { merchantIds = receipt.MerchantIds });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao persistir webhook iFood para empresa {CompanyId}.", companyId);
            return WebhookError(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private ObjectResult WebhookError(int statusCode) => StatusCode(statusCode, new
    {
        error = statusCode switch
        {
            400 => "Estrutura do webhook inválida.",
            401 => "Assinatura do webhook ausente ou inválida.",
            403 => "Loja não vinculada à integração.",
            413 => "Corpo do webhook excede o limite permitido.",
            _ => "Webhook indisponível. Verifique modo de recebimento, credenciais e logs da API."
        }
    });
}
