using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.Update;

internal sealed class UpdateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IJobTitleRepository jobTitleRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateEmployeeCommand>
{
    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (employee is null || !employee.IsActive)
            return Result.Failure(new Error("Employee.NotFound", "Employee not found."));

        var jobTitle = await jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
        if (jobTitle is null || !jobTitle.IsActive)
            return Result.Failure(new Error("JobTitle.NotFound", "Job title not found."));

        var result = employee.UpdateDetails(request.JobTitleId, request.Name, request.Email, request.Phone, request.Salary);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
