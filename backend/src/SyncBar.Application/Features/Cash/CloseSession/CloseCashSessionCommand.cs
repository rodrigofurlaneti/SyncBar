using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Cash.CloseSession;

public sealed record CloseCashSessionCommand(
    long CashSessionId,
    long ClosedByEmployeeId,
    decimal ClosingAmount) : ICommand<CloseCashSessionResponse>;
