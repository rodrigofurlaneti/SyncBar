using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Billing.RegisterSale;

public sealed record SalePaymentInput(
    long PaymentMethodId,
    decimal Amount,
    decimal? ChangeAmount,
    string? AuthorizationCode);

public sealed record RegisterSaleCommand(
    long CustomerOrderId,
    long CashSessionId,
    long EmployeeId,
    IReadOnlyCollection<SalePaymentInput> Payments) : ICommand<long>;
