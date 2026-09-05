using MediatR;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Checkout.PayOrderWithBoleto
{
    internal sealed class PayOrderWithBoletoCommandHandler
        : BaseCommandHandler<PayOrderWithBoletoCommand, PayOrderWithBoletoResponse>
    {
        private readonly ICheckoutOrderPreparer _checkoutPreparer;
        private readonly IAsaasIntegrationPaymentRepository _asaasPaymentRepository;
        private readonly IAsaasService _asaasService;
        private readonly ISender _mediator;

        public PayOrderWithBoletoCommandHandler(
            ICheckoutOrderPreparer checkoutPreparer,
            IAsaasIntegrationPaymentRepository asaasPaymentRepository,
            IAsaasService asaasService,
            ISender mediator,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _checkoutPreparer = checkoutPreparer;
            _asaasPaymentRepository = asaasPaymentRepository;
            _asaasService = asaasService;
            _mediator = mediator;
        }

        public override async Task<Result<PayOrderWithBoletoResponse>> Handle(
            PayOrderWithBoletoCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(PayOrderWithBoletoCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // 1. Fecha o pedido (se ainda aberto) e garante cliente + vínculo Asaas
                    var preparationResult = await _checkoutPreparer.PrepareAsync(request.CustomerOrderId, cancellationToken);
                    if (preparationResult.IsFailure)
                        return Result.Failure<PayOrderWithBoletoResponse>(preparationResult.Error);

                    var order = preparationResult.Value.Order;

                    // 2. Idempotência — reaproveita cobrança pendente em vez de gerar outra no Asaas
                    var existingPayment = await _asaasPaymentRepository.GetByCustomerOrderIdAsync(order.Id, cancellationToken);
                    if (existingPayment is not null)
                    {
                        if (string.Equals(existingPayment.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                        {
                            var (identificationField, barCode) = await TryGetIdentificationFieldAsync(
                                existingPayment.AsaasPaymentId, cancellationToken);

                            return Result.Success(new PayOrderWithBoletoResponse(
                                existingPayment.Id,
                                existingPayment.AsaasPaymentId,
                                existingPayment.Status,
                                existingPayment.BankSlipUrl,
                                identificationField,
                                barCode,
                                existingPayment.Value,
                                existingPayment.DueDate));
                        }

                        return Result.Failure<PayOrderWithBoletoResponse>(
                            Error.Conflict("Asaas.PaymentAlreadyExists", "Este pedido já possui uma cobrança Asaas registrada."));
                    }

                    // 3. Emite o boleto reaproveitando o comando já existente (cobra + persiste)
                    var dueDate = DateTime.UtcNow.AddDays(3);
                    var paymentResult = await _mediator.Send(
                        new CreateAsaasIntegrationPaymentCommand(
                            order.BranchId,
                            order.Id,
                            order.CustomerId,
                            "BOLETO",
                            order.TotalAmount,
                            dueDate),
                        cancellationToken);

                    if (paymentResult.IsFailure)
                        return Result.Failure<PayOrderWithBoletoResponse>(paymentResult.Error);

                    var payment = paymentResult.Value;

                    // 4. Busca a linha digitável — não é persistida (a Asaas recomenda buscar sob demanda)
                    var (newIdentificationField, newBarCode) = await TryGetIdentificationFieldAsync(payment.AsaasPaymentId, cancellationToken);

                    return Result.Success(new PayOrderWithBoletoResponse(
                        payment.PaymentId,
                        payment.AsaasPaymentId,
                        payment.Status,
                        payment.BankSlipUrl,
                        newIdentificationField,
                        newBarCode,
                        order.TotalAmount,
                        dueDate));
                });
        }

        private async Task<(string? IdentificationField, string? BarCode)> TryGetIdentificationFieldAsync(
            string asaasPaymentId, CancellationToken cancellationToken)
        {
            try
            {
                var field = await _asaasService.GetBoletoIdentificationFieldAsync(asaasPaymentId, cancellationToken);
                return (field.IdentificationField, field.BarCode);
            }
            catch (HttpRequestException)
            {
                // A linha digitável pode ser consultada novamente depois; não interrompe o fluxo.
                return (null, null);
            }
        }
    }
}
