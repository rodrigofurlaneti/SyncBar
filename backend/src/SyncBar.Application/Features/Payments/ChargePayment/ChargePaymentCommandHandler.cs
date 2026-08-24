using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Payments;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Payments.ChargePayment;

internal sealed class ChargePaymentCommandHandler(
    IPaymentGatewayService gateway,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<ChargePaymentCommand, ChargePaymentResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<ChargePaymentResponse>> Handle(ChargePaymentCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ChargePaymentCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário ou cliente responsável pela transação, preencha:

                var charge = await gateway.ChargeAsync(
                    new PaymentChargeRequest(request.SaleId, request.Amount, request.Method, request.CustomerDocument),
                    cancellationToken);

                if (charge.Status == PaymentChargeStatus.Declined)
                    return Result.Failure<ChargePaymentResponse>(
                        new Error("Payment.Declined", charge.FailureReason ?? "Payment declined by gateway."));

                return Result.Success(new ChargePaymentResponse(
                    charge.GatewayTransactionId, charge.Status.ToString(), charge.QrCodePayload));
            });
    }
}