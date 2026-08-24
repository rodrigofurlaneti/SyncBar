using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.SetCommission;

internal sealed class SetCommissionCommandHandler : BaseCommandHandler<SetCommissionCommand>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetCommissionCommandHandler(
        IEmployeeRepository employeeRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SetCommissionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetCommissionCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var employee = await _employeeRepository.GetByIdForUpdateAsync(request.EmployeeId, cancellationToken);
                if (employee is null || !employee.IsActive)
                    return Result.Failure(new Error("Employee.NotFound", "Employee not found."));

                var result = employee.SetCommissionPercent(request.CommissionPercent);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}