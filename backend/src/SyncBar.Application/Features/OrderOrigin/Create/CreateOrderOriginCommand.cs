using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.OrderOrigin.Create
{
    public sealed record CreateOrderOriginCommand(
        long? CompanyId,
        long? BranchId,
        string Name) : ICommand<long>;
}
