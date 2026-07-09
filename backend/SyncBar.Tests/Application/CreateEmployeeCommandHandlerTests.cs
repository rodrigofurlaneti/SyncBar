using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Employees.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class CreateEmployeeCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IJobTitleRepository _jobTitleRepository = Substitute.For<IJobTitleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateEmployeeCommandHandler CreateHandler()
        => new(_employeeRepository, _jobTitleRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateEmployee()
    {
        _employeeRepository.ExistsByCpfAsync("12345678901", Arg.Any<CancellationToken>()).Returns(false);
        _jobTitleRepository.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(JobTitle.Create(1, "Garçom").Value);

        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand(1, 2, "João Silva", "12345678901", null, null, DateTime.UtcNow, 2500m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _employeeRepository.Received(1).AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateCpf_ShouldFail()
    {
        _employeeRepository.ExistsByCpfAsync("12345678901", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand(1, 2, "João Silva", "12345678901", null, null, DateTime.UtcNow, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.CpfAlreadyExists");
        await _employeeRepository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
    }
}
