using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.Dismiss;

internal sealed class DismissEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DismissEmployeeCommand>
{
    public async Task<Result> Handle(DismissEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
            return Result.Failure(new Error("Employee.NotFound", "Employee not found."));

        // Demissao e soft delete: DismissedAt + IsActive = 0 (CPF liberado pelo indice filtrado).
        var result = employee.Dismiss();
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
