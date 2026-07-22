using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Cash.OpenSession;

public sealed record OpenCashSessionCommand(
    long CashRegisterId,
    long OpenedByEmployeeId,
    decimal OpeningAmount) : ICommand<long>;
