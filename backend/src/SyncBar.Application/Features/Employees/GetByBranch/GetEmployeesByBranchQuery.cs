using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Employees.GetByBranch;

public sealed record GetEmployeesByBranchQuery(long BranchId) : IQuery<IReadOnlyCollection<EmployeeResponse>>;
