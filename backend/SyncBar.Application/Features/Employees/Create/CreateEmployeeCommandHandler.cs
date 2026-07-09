using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.Create;

internal sealed class CreateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IJobTitleRepository jobTitleRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateEmployeeCommand, long>
{
    public async Task<Result<long>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // CPF unico entre ativos (espelha UQ_Employee_Cpf filtrado).
        if (await employeeRepository.ExistsByCpfAsync(request.Cpf, cancellationToken))
            return Result.Failure<long>(new Error("Employee.CpfAlreadyExists", "An active employee with this CPF already exists."));

        var jobTitle = await jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
        if (jobTitle is null || !jobTitle.IsActive)
            return Result.Failure<long>(new Error("JobTitle.NotFound", "Job title not found."));

        var employee = Employee.Create(
            request.BranchId, request.JobTitleId, request.Name, request.Cpf,
            request.Email, request.Phone, request.HiredAt, null, request.Salary);
        if (employee.IsFailure)
            return Result.Failure<long>(employee.Error);

        await employeeRepository.AddAsync(employee.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(employee.Value.Id);
    }
}
