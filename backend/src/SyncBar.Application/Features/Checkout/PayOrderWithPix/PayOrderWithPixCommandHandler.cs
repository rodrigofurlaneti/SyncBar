using MediatR;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Checkout.PayOrderWithPix
{
    internal sealed class PayOrderWithPixCommandHandler
        : BaseCommandHandler<PayOrderWithPixCommand, PayOrderWithPixResponse>
    {
        private readonly ICheckoutOrderPreparer _checkoutPreparer;
        private readonly IAsaasIntegrationPaymentRepository _asaasPaymentRepository;
        private readonly ISender _mediator;

        public PayOrderWithPixCommandHandler(
            ICheckoutOrderPreparer checkoutPreparer,
            IAsaasIntegrationPaymentRepository asaasPaymentRepository,
            ISender mediator,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _checkoutPreparer = checkoutPreparer;
            _asaasPaymentRepository = asaasPaymentRepository;
            _mediator = mediator;
        }

        public override async Task<Result<PayOrderWithPixResponse>> Handle(
            PayOrderWithPixCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(PayOrderWithPixCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // 1. Fecha o pedido (se ainda aberto) e garante cliente + vínculo Asaas
                    var preparationResult = await _checkoutPreparer.PrepareAsync(request.CustomerOrderId, cancellationToken);
                    if (preparationResult.IsFailure)
                        return Result.Failure<PayOrderWithPixResponse>(preparationResult.Error);

                    var order = preparationResult.Value.Order;

                    // 2. Idempotência — reaproveita cobrança pendente em vez de gerar outra no Asaas
                    var existingPayment = await _asaasPaymentRepository.GetByCustomerOrderIdAsync(order.Id, cancellationToken);
                    if (existingPayment is not null)
                    {
                        if (string.Equals(existingPayment.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                        {
                            return Result.Success(new PayOrderWithPixResponse(
                                existingPayment.Id,
                                existingPayment.AsaasPaymentId,
                                existingPayment.Status,
                                existingPayment.PixQrCodeBase64,
                                existingPayment.PixPayload,
                                existingPayment.InvoiceUrl,
                                existingPayment.Value));
                        }

                        return Result.Failure<PayOrderWithPixResponse>(
                            Error.Conflict("Asaas.PaymentAlreadyExists", "Este pedido já possui uma cobrança Asaas registrada."));
                    }

                    // 3. Emite a cobrança Pix reaproveitando o comando já existente (cobra + busca QR Code + persiste)
                    var paymentResult = await _mediator.Send(
                        new CreateAsaasIntegrationPaymentCommand(
                            order.BranchId,
                            order.Id,
                            order.CustomerId,
                            "PIX",
                            order.TotalAmount,
                            DateTime.UtcNow.AddHours(1)),
                        cancellationToken);

                    if (paymentResult.IsFailure)
                        return Result.Failure<PayOrderWithPixResponse>(paymentResult.Error);

                    var payment = paymentResult.Value;
                    return Result.Success(new PayOrderWithPixResponse(
                        payment.PaymentId,
                        payment.AsaasPaymentId,
                        payment.Status,
                        payment.PixQrCodeBase64,
                        payment.PixPayload,
                        payment.InvoiceUrl,
                        order.TotalAmount));
                });
        }
    }
}
