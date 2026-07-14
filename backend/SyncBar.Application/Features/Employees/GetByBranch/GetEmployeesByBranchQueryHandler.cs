using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.GetByBranch;

internal sealed class GetEmployeesByBranchQueryHandler(IEmployeeRepository employeeRepository)
    : IQueryHandler<GetEmployeesByBranchQuery, IReadOnlyCollection<EmployeeResponse>>
{
    public async Task<Result<IReadOnlyCollection<EmployeeResponse>>> Handle(
        GetEmployeesByBranchQuery request, CancellationToken cancellationToken)
    {
        var employees = await employeeRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        IReadOnlyCollection<EmployeeResponse> response = employees
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeResponse(
                e.Id, e.BranchId, e.JobTitleId, e.Name, e.Cpf, e.Email, e.Phone,
                e.HiredAt, e.DismissedAt, e.Salary, e.CommissionPercent, e.IsActive))
            .ToList();

        return Result.Success(response);
    }
}
