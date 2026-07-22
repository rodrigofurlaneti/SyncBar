using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Users.Deactivate;

public sealed record DeactivateUserCommand(long AppUserId) : ICommand;
