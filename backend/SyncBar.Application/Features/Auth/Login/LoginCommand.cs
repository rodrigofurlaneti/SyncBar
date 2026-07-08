using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Auth.Login;

public sealed record LoginCommand(string UserName, string Password) : ICommand<LoginResponse>;
