using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Employees.GetByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.GetByBranch;

public sealed class GetEmployeesByBranchQueryHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetEmployeesByBranchQueryHandler _handler;

    public GetEmployeesByBranchQueryHandlerTests()
    {
        _handler = new GetEmployeesByBranchQueryHandler(_employeeRepository, _logRepository, _unitOfWork);
    }

    private static Employee CreateEmployee(string name, long branchId = 1, long jobTitleId = 1, string cpf = "12345678900")
        => Employee.Create(branchId, jobTitleId, name, cpf, "func@teste.com", "11999990000", DateTime.Now, null, 1500m).Value;

    [Fact]
    public async Task Handle_NoEmployeesForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetEmployeesByBranchQuery(BranchId: 1);
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleEmployees_ShouldOrderByNameAndMapAllFields()
    {
        var query = new GetEmployeesByBranchQuery(BranchId: 1);
        var employeeBeatriz = CreateEmployee("Beatriz");
        var employeeAna = CreateEmployee("Ana");
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([employeeBeatriz, employeeAna]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.Name).Should().ContainInOrder("Ana", "Beatriz");

        var firstResponse = result.Value.First();
        firstResponse.Id.Should().Be(employeeAna.Id);
        firstResponse.BranchId.Should().Be(employeeAna.BranchId);
        firstResponse.JobTitleId.Should().Be(employeeAna.JobTitleId);
        firstResponse.Name.Should().Be(employeeAna.Name);
        firstResponse.Cpf.Should().Be(employeeAna.Cpf);
        firstResponse.Email.Should().Be(employeeAna.Email);
        firstResponse.Phone.Should().Be(employeeAna.Phone);
        firstResponse.HiredAt.Should().Be(employeeAna.HiredAt);
        firstResponse.DismissedAt.Should().Be(employeeAna.DismissedAt);
        firstResponse.Salary.Should().Be(employeeAna.Salary);
        firstResponse.CommissionPercent.Should().Be(employeeAna.CommissionPercent);
        firstResponse.IsActive.Should().Be(employeeAna.IsActive);
    }
}
