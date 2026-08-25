using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Assignment.Create
{
    public sealed record CreateDiningAreaAssignmentCommand(
        long DiningAreaId,
        long EmployeeId,
        DateTime StartAt) : ICommand<long>;
}
