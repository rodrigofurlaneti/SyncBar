using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Employees.Dismiss;

public sealed record DismissEmployeeCommand(long EmployeeId) : ICommand;
