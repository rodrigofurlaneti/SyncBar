using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.SetCommission;

internal sealed class SetCommissionCommandHandler(IEmployeeRepository employeeRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<SetCommissionCommand>
{
    public async Task<Result> Handle(SetCommissionCommand request, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (employee is null || !employee.IsActive)
            return Result.Failure(new Error("Employee.NotFound", "Employee not found."));

        var result = employee.SetCommissionPercent(request.CommissionPercent);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
