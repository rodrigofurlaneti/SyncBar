using MediatR;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Checkout.PayOrderWithCreditCard
{
    internal sealed class PayOrderWithCreditCardCommandHandler
        : BaseCommandHandler<PayOrderWithCreditCardCommand, PayOrderWithCreditCardResponse>
    {
        private readonly ICheckoutOrderPreparer _checkoutPreparer;
        private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository;
        private readonly IAsaasIntegrationPaymentRepository _asaasPaymentRepository;
        private readonly IAsaasService _asaasService;
        private readonly ISender _mediator;

        public PayOrderWithCreditCardCommandHandler(
            ICheckoutOrderPreparer checkoutPreparer,
            IAsaasIntegrationSavedCardRepository savedCardRepository,
            IAsaasIntegrationPaymentRepository asaasPaymentRepository,
            IAsaasService asaasService,
            ISender mediator,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _checkoutPreparer = checkoutPreparer;
            _savedCardRepository = savedCardRepository;
            _asaasPaymentRepository = asaasPaymentRepository;
            _asaasService = asaasService;
            _mediator = mediator;
        }

        public override async Task<Result<PayOrderWithCreditCardResponse>> Handle(
            PayOrderWithCreditCardCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(PayOrderWithCreditCardCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // 1. Fecha o pedido (se ainda aberto) e garante cliente + vínculo Asaas
                    var preparationResult = await _checkoutPreparer.PrepareAsync(request.CustomerOrderId, cancellationToken);
                    if (preparationResult.IsFailure)
                        return Result.Failure<PayOrderWithCreditCardResponse>(preparationResult.Error);

                    var (order, customer, branch, asaasCustomerId) = preparationResult.Value;

                    // 2. Idempotência — reaproveita cobrança pendente em vez de gerar outra no Asaas
                    var existingPayment = await _asaasPaymentRepository.GetByCustomerOrderIdAsync(order.Id, cancellationToken);
                    if (existingPayment is not null)
                    {
                        if (string.Equals(existingPayment.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                        {
                            return Result.Success(new PayOrderWithCreditCardResponse(
                                existingPayment.Id,
                                existingPayment.AsaasPaymentId,
                                existingPayment.Status,
                                null,
                                null,
                                existingPayment.Value));
                        }

                        return Result.Failure<PayOrderWithCreditCardResponse>(
                            Error.Conflict("Asaas.PaymentAlreadyExists", "Este pedido já possui uma cobrança Asaas registrada."));
                    }

                    // 3. Resolve o token do cartão: salvo existente, cartão novo (salvando) ou cartão novo avulso
                    string creditCardToken;
                    string? cardBrand = null;
                    string? last4Digits = null;

                    if (request.SavedCardId.HasValue)
                    {
                        var savedCard = await _savedCardRepository.GetByIdAsync(request.SavedCardId.Value, cancellationToken);
                        if (savedCard is null)
                            return Result.Failure<PayOrderWithCreditCardResponse>(
                                Error.NotFound("SavedCard.NotFound", "Cartão salvo não encontrado."));

                        if (savedCard.CustomerId != customer.Id || savedCard.CompanyId != branch.CompanyId)
                            return Result.Failure<PayOrderWithCreditCardResponse>(
                                Error.Validation("SavedCard.NotOwnedByCustomer", "Este cartão salvo não pertence ao cliente do pedido."));

                        creditCardToken = savedCard.CreditCardToken;
                        cardBrand = savedCard.CardBrand;
                        last4Digits = savedCard.Last4Digits;
                    }
                    else if (request.SaveCard)
                    {
                        var card = request.Card!;
                        var saveResult = await _mediator.Send(
                            new CreateAsaasIntegrationSavedCardCommand(
                                customer.Id, branch.CompanyId, card.HolderName, card.Number, card.ExpiryMonth, card.ExpiryYear, card.Ccv,
                                BranchId: order.BranchId),
                            cancellationToken);

                        if (saveResult.IsFailure)
                            return Result.Failure<PayOrderWithCreditCardResponse>(saveResult.Error);

                        var savedCard = await _savedCardRepository.GetByIdAsync(saveResult.Value.Id, cancellationToken);
                        if (savedCard is null)
                            return Result.Failure<PayOrderWithCreditCardResponse>(
                                Error.Failure("SavedCard.PersistenceFailed", "Cartão tokenizado, mas não foi possível recuperá-lo para cobrança."));

                        creditCardToken = savedCard.CreditCardToken;
                        cardBrand = savedCard.CardBrand;
                        last4Digits = savedCard.Last4Digits;
                    }
                    else
                    {
                        var card = request.Card!;
                        try
                        {
                            var tokenized = await _asaasService.TokenizeCreditCardAsync(
                                asaasCustomerId,
                                new CreditCardRequest(card.HolderName, card.Number, card.ExpiryMonth, card.ExpiryYear, card.Ccv),
                                holderInfo: null,
                                cancellationToken);

                            creditCardToken = tokenized.CreditCardToken;
                            cardBrand = tokenized.CreditCardBrand;
                            last4Digits = tokenized.CreditCardNumber.Length >= 4
                                ? tokenized.CreditCardNumber[^4..]
                                : tokenized.CreditCardNumber;
                        }
                        catch (HttpRequestException ex)
                        {
                            return Result.Failure<PayOrderWithCreditCardResponse>(
                                Error.Failure("AsaasApi.TokenizeCardFailed", $"Falha ao processar o cartão: {ex.Message}"));
                        }
                    }

                    // 4. Cobra reaproveitando o comando já existente (cobra com o token + persiste)
                    var paymentResult = await _mediator.Send(
                        new CreateAsaasIntegrationPaymentCommand(
                            order.BranchId,
                            order.Id,
                            order.CustomerId,
                            "CREDIT_CARD",
                            order.TotalAmount,
                            DateTime.UtcNow,
                            InstallmentCount: 1,
                            CreditCardToken: creditCardToken),
                        cancellationToken);

                    if (paymentResult.IsFailure)
                        return Result.Failure<PayOrderWithCreditCardResponse>(paymentResult.Error);

                    var payment = paymentResult.Value;
                    return Result.Success(new PayOrderWithCreditCardResponse(
                        payment.PaymentId,
                        payment.AsaasPaymentId,
                        payment.Status,
                        cardBrand,
                        last4Digits,
                        order.TotalAmount));
                });
        }
    }
}
