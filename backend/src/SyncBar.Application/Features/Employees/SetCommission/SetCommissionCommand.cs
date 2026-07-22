using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Employees.SetCommission;

public sealed record SetCommissionCommand(long EmployeeId, decimal? CommissionPercent) : ICommand;
