using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.OrderOrigin.Remove
{
    public sealed record RemoveOrderOriginCommand(long Id) : ICommand;
}
