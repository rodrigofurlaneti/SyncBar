using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Payments;

namespace SyncBar.Application.Features.Payments.ChargePayment;

public sealed record ChargePaymentCommand(
    long SaleId,
    decimal Amount,
    PaymentGatewayMethod Method,
    string? CustomerDocument) : ICommand<ChargePaymentResponse>;

public sealed record ChargePaymentResponse(
    string GatewayTransactionId,
    string Status,
    string? QrCodePayload);
