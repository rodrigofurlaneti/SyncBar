using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Cash.ReviewSession;

public sealed record ReviewCashSessionCommand(long CashSessionId) : ICommand;
