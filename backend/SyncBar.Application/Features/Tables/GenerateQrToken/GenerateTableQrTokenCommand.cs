using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Tables.GenerateQrToken;

public sealed record GenerateTableQrTokenCommand(long DiningTableId) : ICommand<Guid>;
