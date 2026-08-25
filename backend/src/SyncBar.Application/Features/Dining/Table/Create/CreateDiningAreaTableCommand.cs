using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Table.Create
{
    public sealed record CreateDiningAreaTableCommand(
        long DiningAreaId,
        long DiningTableId) : ICommand<long>;
}
