using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Table.Deactivate
{
    public sealed record DeactivateDiningAreaTableCommand(long Id) : ICommand;
}
