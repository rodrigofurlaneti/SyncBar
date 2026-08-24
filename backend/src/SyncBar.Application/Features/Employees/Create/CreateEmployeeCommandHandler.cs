using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.Create;

internal sealed class CreateEmployeeCommandHandler : BaseCommandHandler<CreateEmployeeCommand, long>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IJobTitleRepository _jobTitleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmployeeCommandHandler(
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

    public override async Task<Result<long>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateEmployeeCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // CPF unico entre ativos (espelha UQ_Employee_Cpf filtrado).
                if (await _employeeRepository.ExistsByCpfAsync(request.Cpf, cancellationToken))
                    return Result.Failure<long>(new Error("Employee.CpfAlreadyExists", "An active employee with this CPF already exists."));

                var jobTitle = await _jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
                if (jobTitle is null || !jobTitle.IsActive)
                    return Result.Failure<long>(new Error("JobTitle.NotFound", "Job title not found."));

                var employee = Employee.Create(
                    request.BranchId, request.JobTitleId, request.Name, request.Cpf,
                    request.Email, request.Phone, request.HiredAt, null, request.Salary);
                if (employee.IsFailure)
                    return Result.Failure<long>(employee.Error);

                await _employeeRepository.AddAsync(employee.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(employee.Value.Id);
            });
    }
}