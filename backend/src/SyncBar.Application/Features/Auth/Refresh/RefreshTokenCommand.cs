using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Auth.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<LoginResponse>;
