using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Area.Update
{
    public sealed record UpdateDiningAreaCommand(
        long Id,
        string Name) : ICommand;
}
