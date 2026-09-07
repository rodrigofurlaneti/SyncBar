using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.OrderOrigin.Update
{
    public sealed record UpdateOrderOriginCommand(
        long Id,
        long? CompanyId,
        long? BranchId,
        string Name,
        bool IsActive) : ICommand;
}
