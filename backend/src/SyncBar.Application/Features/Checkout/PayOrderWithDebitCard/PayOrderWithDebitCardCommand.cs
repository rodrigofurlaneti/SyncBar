using MediatR;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Cash;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Checkout.PayOrderWithDebitCard;

public sealed record PayOrderWithDebitCardCommand(long CustomerOrderId) : ICommand<HostedCardPaymentResponse>;
public sealed record HostedCardPaymentResponse(long PaymentId, string AsaasPaymentId, string Status, string InvoiceUrl, decimal Value);

internal sealed class PayOrderWithDebitCardCommandHandler(ICheckoutOrderPreparer preparer,
    IPaymentMethodAvailability availability, IAsaasIntegrationPaymentRepository payments, ISender sender,
    ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseCommandHandler<PayOrderWithDebitCardCommand, HostedCardPaymentResponse>(logs, unitOfWork)
{
    public override Task<Result<HostedCardPaymentResponse>> Handle(PayOrderWithDebitCardCommand request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(PayOrderWithDebitCardCommandHandler), nameof(Handle), null, async _ =>
        {
            var enabled = await availability.ValidateOrderAsync(request.CustomerOrderId, [3], ct);
            if (enabled.IsFailure) return Result.Failure<HostedCardPaymentResponse>(enabled.Error);
            var preparation = await preparer.PrepareAsync(request.CustomerOrderId, ct);
            if (preparation.IsFailure) return Result.Failure<HostedCardPaymentResponse>(preparation.Error);
            var order = preparation.Value.Order;
            var allowed = await availability.ValidateAsync(order.BranchId, [3], ct);
            if (allowed.IsFailure) return Result.Failure<HostedCardPaymentResponse>(allowed.Error);
            var existing = await payments.GetByCustomerOrderIdAsync(order.Id, ct);
            if (existing is not null)
            {
                if (existing.BillingType != "CREDIT_CARD" || existing.Status != "PENDING" || string.IsNullOrWhiteSpace(existing.InvoiceUrl))
                    return Result.Failure<HostedCardPaymentResponse>(new Error("Asaas.PaymentAlreadyExists", "Este pedido já possui uma cobrança. Consulte o pagamento existente."));
                return Result.Success(new HostedCardPaymentResponse(existing.Id, existing.AsaasPaymentId, existing.Status, existing.InvoiceUrl, existing.Value));
            }
            var created = await sender.Send(new CreateAsaasIntegrationPaymentCommand(order.BranchId, order.Id,
                order.CustomerId, "CREDIT_CARD", order.TotalAmount, DateTime.Today, UseHostedCheckout: true), ct);
            if (created.IsFailure) return Result.Failure<HostedCardPaymentResponse>(created.Error);
            if (string.IsNullOrWhiteSpace(created.Value.InvoiceUrl))
                return Result.Failure<HostedCardPaymentResponse>(new Error("Asaas.InvoiceUnavailable", "A fatura ainda não está disponível. Consulte o pagamento novamente."));
            return Result.Success(new HostedCardPaymentResponse(created.Value.PaymentId, created.Value.AsaasPaymentId,
                created.Value.Status, created.Value.InvoiceUrl, order.TotalAmount));
        });
}
