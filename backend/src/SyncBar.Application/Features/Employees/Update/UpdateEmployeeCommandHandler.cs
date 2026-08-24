using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.Update;

internal sealed class UpdateEmployeeCommandHandler : BaseCommandHandler<UpdateEmployeeCommand>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IJobTitleRepository _jobTitleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IJobTitleRepository jobTitleRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _jobTitleRepository = jobTitleRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UpdateEmployeeCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var employee = await _employeeRepository.GetByIdForUpdateAsync(request.EmployeeId, cancellationToken);
                if (employee is null || !employee.IsActive)
                    return Result.Failure(new Error("Employee.NotFound", "Employee not found."));

                var jobTitle = await _jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
                if (jobTitle is null || !jobTitle.IsActive)
                    return Result.Failure(new Error("JobTitle.NotFound", "Job title not found."));

                var result = employee.UpdateDetails(request.JobTitleId, request.Name, request.Email, request.Phone, request.Salary);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}