using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Checkout.PayOrderWithBoleto;
using SyncBar.Application.Features.Checkout.PayOrderWithCreditCard;
using SyncBar.Application.Features.Checkout.PayOrderWithPix;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers
{
    [AllowAnonymous]
    [Route("api/checkout")]
    public sealed class CheckoutController(
        IMediator mediator,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork) : ApiController(mediator)
    {
        [HttpPost("debito")]
        public async Task<IActionResult> PayWithDebit([FromBody] CheckoutPixRequest request, CancellationToken ct)
        {
            if (request.CustomerOrderId is null or <= 0) return BadRequest(new { message = "Pedido obrigatório." });
            var result = await Mediator.Send(new SyncBar.Application.Features.Checkout.PayOrderWithDebitCard.PayOrderWithDebitCardCommand(request.CustomerOrderId.Value), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        }

        [HttpPost("pix")]
        public Task<IActionResult> PayWithPix([FromBody] CheckoutPixRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CheckoutController), nameof(PayWithPix), async () =>
            {
                if (request.CustomerOrderId is null)
                    return BadRequest(new { message = "CustomerOrderId is required." });

                var result = await Mediator.Send(new PayOrderWithPixCommand(request.CustomerOrderId.Value), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpPost("cartao")]
        public Task<IActionResult> PayWithCreditCard([FromBody] CheckoutCreditCardRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CheckoutController), nameof(PayWithCreditCard), async () =>
            {
                if (request.CustomerOrderId is null)
                    return BadRequest(new { message = "CustomerOrderId is required." });

                var result = await Mediator.Send(
                    new PayOrderWithCreditCardCommand(request.CustomerOrderId.Value, request.SavedCardId, request.Card, request.SaveCard),
                    ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpPost("boleto")]
        public Task<IActionResult> PayWithBoleto([FromBody] CheckoutBoletoRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CheckoutController), nameof(PayWithBoleto), async () =>
            {
                if (request.CustomerOrderId is null)
                    return BadRequest(new { message = "CustomerOrderId is required." });

                var result = await Mediator.Send(new PayOrderWithBoletoCommand(request.CustomerOrderId.Value), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });
    }

    public sealed record CheckoutPixRequest(long? CustomerOrderId);

    public sealed record CheckoutCreditCardRequest(
        long? CustomerOrderId,
        long? SavedCardId,
        CreditCardDataRequest? Card,
        bool SaveCard = false);

    public sealed record CheckoutBoletoRequest(long? CustomerOrderId);
}
