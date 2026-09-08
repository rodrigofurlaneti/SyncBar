using SyncBar.Application.Features.Cash;
using MediatR;
using SyncBar.Application.Abstractions.Integrations.Asaas;
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
        private readonly IAsaasService _asaasService;
        private readonly ISender _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentMethodAvailability _availability;

        public PayOrderWithPixCommandHandler(
            ICheckoutOrderPreparer checkoutPreparer,
            IAsaasIntegrationPaymentRepository asaasPaymentRepository,
            IAsaasService asaasService,
            ISender mediator,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork, IPaymentMethodAvailability availability)
            : base(logRepository, unitOfWork)
        {
            _checkoutPreparer = checkoutPreparer;
            _asaasPaymentRepository = asaasPaymentRepository;
            _asaasService = asaasService;
            _mediator = mediator;
            _unitOfWork = unitOfWork; _availability = availability;
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
                    var enabled = await _availability.ValidateOrderAsync(request.CustomerOrderId, [4], cancellationToken);
                    if (enabled.IsFailure) return Result.Failure<PayOrderWithPixResponse>(enabled.Error);
                    var preparationResult = await _checkoutPreparer.PrepareAsync(request.CustomerOrderId, cancellationToken);
                    if (preparationResult.IsFailure)
                        return Result.Failure<PayOrderWithPixResponse>(preparationResult.Error);

                    var order = preparationResult.Value.Order;
                    var available = await _availability.ValidateAsync(order.BranchId, [4], cancellationToken);
                    if (available.IsFailure) return Result.Failure<PayOrderWithPixResponse>(available.Error);

                    // 2. Idempotência — reaproveita cobrança pendente em vez de gerar outra no Asaas
                    var existingPayment = await _asaasPaymentRepository.GetByCustomerOrderIdAsync(order.Id, cancellationToken);
                    if (existingPayment is not null)
                    {
                        if (string.Equals(existingPayment.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                        {
                            var qrCodeBase64 = existingPayment.PixQrCodeBase64;
                            var pixPayload = existingPayment.PixPayload;

                            // Uma tentativa anterior pode ter criado a cobrança no Asaas com sucesso mas
                            // falhado ao buscar o QR Code (hiccup pontual do gateway) — sem isto, a
                            // idempotência acima devolveria pra sempre o mesmo registro sem QR Code,
                            // já que nunca mais tentaria buscar de novo (a tela do storefront ficaria
                            // presa mostrando só "Aguardando confirmação...", sem nunca exibir o QR).
                            if (string.IsNullOrEmpty(qrCodeBase64) &&
                                string.Equals(existingPayment.BillingType, "PIX", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var qrCode = await _asaasService.GetPixQrCodeAsync(existingPayment.AsaasPaymentId, cancellationToken);
                                    qrCodeBase64 = qrCode.EncodedImage;
                                    pixPayload = qrCode.Payload;

                                    var trackedPayment = await _asaasPaymentRepository.GetByIdForUpdateAsync(existingPayment.Id, cancellationToken);
                                    if (trackedPayment is not null)
                                    {
                                        trackedPayment.SetPixDetails(qrCodeBase64, pixPayload);
                                        await _unitOfWork.CommitAsync(cancellationToken);
                                    }
                                }
                                catch (HttpRequestException)
                                {
                                    // Ainda indisponível no Asaas — devolve sem QR Code; o cliente pode
                                    // tentar novamente (reabrir o pagamento repete este mesmo fluxo).
                                }
                            }

                            return Result.Success(new PayOrderWithPixResponse(
                                existingPayment.Id,
                                existingPayment.AsaasPaymentId,
                                existingPayment.Status,
                                qrCodeBase64,
                                pixPayload,
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


