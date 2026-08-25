using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Assignment.Deactivate
{
    public sealed record DeactivateDiningAreaAssignmentCommand(long Id) : ICommand;
}
