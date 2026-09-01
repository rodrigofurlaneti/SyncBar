using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood;

public sealed record TestIfoodConnectionCommand(long CompanyId) : ICommand<TestIfoodConnectionResponse>;

public sealed record TestIfoodConnectionResponse(bool Success, string? ErrorMessage);
