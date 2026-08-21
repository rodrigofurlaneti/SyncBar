using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood;

public sealed record TestIFoodConnectionCommand(long CompanyId) : ICommand<TestIFoodConnectionResponse>;

public sealed record TestIFoodConnectionResponse(bool Success, string? ErrorMessage);
