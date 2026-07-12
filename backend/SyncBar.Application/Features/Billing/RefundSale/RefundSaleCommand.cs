using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Billing.RefundSale;

public sealed record RefundSaleCommand(long SaleId, long EmployeeId, string? Reason) : ICommand;
