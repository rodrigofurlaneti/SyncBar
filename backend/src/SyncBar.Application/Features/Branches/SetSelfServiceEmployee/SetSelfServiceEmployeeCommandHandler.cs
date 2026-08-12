using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Branches.SetSelfServiceEmployee;

internal sealed class SetSelfServiceEmployeeCommandHandler : BaseCommandHandler<SetSelfServiceEmployeeCommand>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetSelfServiceEmployeeCommandHandler(
        IBranchRepository branchRepository,
        IEmployeeRepository employeeRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _branchRepository = branchRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(SetSelfServiceEmployeeCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(SetSelfServiceEmployeeCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var branch = await _branchRepository.GetByIdForUpdateAsync(request.BranchId, cancellationToken);
                if (branch is null || !branch.IsActive)
                    return Result.Failure(new Error("Branch.NotFound", "Branch not found."));

                if (request.EmployeeId.HasValue)
                {
                    var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId.Value, cancellationToken);
                    if (employee is null || !employee.IsActive || employee.BranchId != request.BranchId)
                        return Result.Failure(new Error("Employee.NotFound", "Employee not found for this branch."));
                }

                branch.SetSelfServiceEmployee(request.EmployeeId);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
}