using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Cash.GetOpenSession;

public sealed record GetOpenSessionQuery(long CashRegisterId) : IQuery<CashSessionResponse>;
