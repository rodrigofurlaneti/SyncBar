using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Payments;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Features.Payments.ChargePayment;

internal sealed class ChargePaymentCommandHandler(IPaymentGatewayService gateway)
    : ICommandHandler<ChargePaymentCommand, ChargePaymentResponse>
{
    public async Task<Result<ChargePaymentResponse>> Handle(ChargePaymentCommand request, CancellationToken cancellationToken)
    {
        var charge = await gateway.ChargeAsync(
            new PaymentChargeRequest(request.SaleId, request.Amount, request.Method, request.CustomerDocument),
            cancellationToken);

        if (charge.Status == PaymentChargeStatus.Declined)
            return Result.Failure<ChargePaymentResponse>(
                new Error("Payment.Declined", charge.FailureReason ?? "Payment declined by gateway."));

        return Result.Success(new ChargePaymentResponse(
            charge.GatewayTransactionId, charge.Status.ToString(), charge.QrCodePayload));
    }
}
