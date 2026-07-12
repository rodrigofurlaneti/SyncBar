using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Comandas.Settings;

public sealed record SetComandaDefaultLimitCommand(long BranchId, decimal DefaultLimitAmount) : ICommand;
