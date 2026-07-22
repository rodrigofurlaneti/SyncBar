namespace SyncBar.Application.Abstractions.Payments;

public enum PaymentGatewayMethod
{
    Pix,
    CreditCard,
    DebitCard
}

public enum PaymentChargeStatus
{
    Pending,
    Approved,
    Declined,
    Refunded
}

public sealed record PaymentChargeRequest(
    long SaleId,
    decimal Amount,
    PaymentGatewayMethod Method,
    string? CustomerDocument = null,
    string? Description = null);

public sealed record PaymentChargeResult(
    string GatewayTransactionId,
    PaymentChargeStatus Status,
    string? QrCodePayload = null,
    string? FailureReason = null);

/// <summary>
/// Abstração para um gateway de pagamento externo (Pix, cartão). A implementação real
/// (MercadoPago, Stripe, PagSeguro, etc.) troca a <see cref="Infrastructure.Payments.FakePaymentGatewayService"/>
/// registrada por padrão — basta implementar esta interface e trocar o registro em
/// SyncBar.Infrastructure.DependencyInjection.AddInfrastructure.
/// </summary>
public interface IPaymentGatewayService
{
    Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);

    Task<PaymentChargeResult> RefundAsync(string gatewayTransactionId, decimal amount, CancellationToken cancellationToken = default);

    Task<PaymentChargeResult> GetStatusAsync(string gatewayTransactionId, CancellationToken cancellationToken = default);
}
