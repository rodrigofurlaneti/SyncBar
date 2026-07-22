using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Branches.SetSelfServiceEmployee;

public sealed record SetSelfServiceEmployeeCommand(long BranchId, long? EmployeeId) : ICommand;
