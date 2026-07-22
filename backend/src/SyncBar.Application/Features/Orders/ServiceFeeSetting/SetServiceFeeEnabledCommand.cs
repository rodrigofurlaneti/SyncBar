using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.ServiceFeeSetting;

public sealed record SetServiceFeeEnabledCommand(long BranchId, bool Enabled) : ICommand;
