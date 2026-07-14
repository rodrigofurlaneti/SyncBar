using SyncBar.Application.Abstractions.Payments;

namespace SyncBar.Infrastructure.Payments;

/// <summary>
/// Implementação fake do gateway de pagamento — não fala com nenhum provedor real.
/// Aprova qualquer cobrança instantaneamente e gera um QR Code Pix falso apenas para
/// permitir testar a UI/fluxo. Substitua por uma implementação real antes de ir para produção.
/// </summary>
internal sealed class FakePaymentGatewayService : IPaymentGatewayService
{
    public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        var transactionId = $"FAKE-{Guid.NewGuid():N}";
        var qrCode = request.Method == PaymentGatewayMethod.Pix
            ? $"00020126FAKE-PIX-PAYLOAD-{transactionId}5204000053039865802BR"
            : null;

        return Task.FromResult(new PaymentChargeResult(
            GatewayTransactionId: transactionId,
            Status: PaymentChargeStatus.Approved,
            QrCodePayload: qrCode));
    }

    public Task<PaymentChargeResult> RefundAsync(string gatewayTransactionId, decimal amount, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentChargeResult(gatewayTransactionId, PaymentChargeStatus.Refunded));

    public Task<PaymentChargeResult> GetStatusAsync(string gatewayTransactionId, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentChargeResult(gatewayTransactionId, PaymentChargeStatus.Approved));
}
