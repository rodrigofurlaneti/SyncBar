using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Area.Create
{
    public sealed record CreateDiningAreaCommand(
        long BranchId,
        string Name) : ICommand<long>;
}
