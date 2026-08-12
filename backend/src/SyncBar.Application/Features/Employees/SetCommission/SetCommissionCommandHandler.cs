using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.SetCommission;

internal sealed class SetCommissionCommandHandler(
    IEmployeeRepository employeeRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetCommissionCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetCommissionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetCommissionCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário responsável pela alteração, preencha:
                // userIdBox.Value = request.UserId;

                var employee = await employeeRepository.GetByIdForUpdateAsync(request.EmployeeId, cancellationToken);
                if (employee is null || !employee.IsActive)
                    return Result.Failure(new Error("Employee.NotFound", "Employee not found."));

                var result = employee.SetCommissionPercent(request.CommissionPercent);
                if (result.IsFailure)
                    return result;

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}