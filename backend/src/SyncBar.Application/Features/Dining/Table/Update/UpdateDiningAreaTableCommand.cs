using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Table.Update
{
    public sealed record UpdateDiningAreaTableCommand(
        long Id,
        long DiningAreaId,
        long DiningTableId) : ICommand;
}
