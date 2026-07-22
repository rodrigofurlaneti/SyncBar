using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Branches.SetSelfServiceEmployee;

internal sealed class SetSelfServiceEmployeeCommandHandler(
    IBranchRepository branchRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetSelfServiceEmployeeCommand>
{
    public async Task<Result> Handle(SetSelfServiceEmployeeCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdForUpdateAsync(request.BranchId, cancellationToken);
        if (branch is null || !branch.IsActive)
            return Result.Failure(new Error("Branch.NotFound", "Branch not found."));

        if (request.EmployeeId.HasValue)
        {
            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId.Value, cancellationToken);
            if (employee is null || !employee.IsActive || employee.BranchId != request.BranchId)
                return Result.Failure(new Error("Employee.NotFound", "Employee not found for this branch."));
        }

        branch.SetSelfServiceEmployee(request.EmployeeId);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
