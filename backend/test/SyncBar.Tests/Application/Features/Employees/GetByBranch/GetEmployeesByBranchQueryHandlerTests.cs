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
    private readonly IAppUserRepository _appUserRepository = Substitute.For<IAppUserRepository>();
    private readonly IAppUserFeatureRepository _appUserFeatureRepository = Substitute.For<IAppUserFeatureRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetEmployeesByBranchQueryHandler _handler;

    public GetEmployeesByBranchQueryHandlerTests()
    {
        // Sem usuário do sistema vinculado por padrão — cada teste que precisar de um
        // AppUser vinculado ao Employee configura GetByEmployeeIdsAsync explicitamente.
        _appUserRepository.GetByEmployeeIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AppUser>());

        _handler = new GetEmployeesByBranchQueryHandler(
            _employeeRepository, _appUserRepository, _appUserFeatureRepository, _logRepository, _unitOfWork);
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
        firstResponse.HasSystemAccess.Should().BeFalse();
        firstResponse.AppUserId.Should().BeNull();
        firstResponse.RoleName.Should().BeNull();
        firstResponse.ExtraFeatureCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_EmployeeWithSystemAccess_ShouldMapRoleNameAndExtraFeatureCount()
    {
        var query = new GetEmployeesByBranchQuery(BranchId: 1);
        var employee = CreateEmployee("Carla");
        _employeeRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([employee]);

        var appUser = AppUser.Create(
            companyId: 1, employeeId: employee.Id, userName: "carla", email: "carla@teste.com", passwordHash: "hash-fake").Value;
        // AppUser.Id só existe após persistência (fábrica usa base(0)) — o handler só lê a
        // contagem da coleção retornada, então o AppUserId usado aqui não precisa bater com
        // appUser.Id; só precisa ser > 0 para passar na validação da própria factory.
        var extraFeatureOne = AppUserFeature.Create(1, 1).Value;
        var extraFeatureTwo = AppUserFeature.Create(1, 2).Value;
        _appUserRepository.GetByEmployeeIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([appUser]);
        _appUserRepository.GetRoleNamesAsync(appUser.Id, Arg.Any<CancellationToken>())
            .Returns(["Gerente"]);
        _appUserFeatureRepository.GetByUserAsync(appUser.Id, Arg.Any<CancellationToken>())
            .Returns([extraFeatureOne, extraFeatureTwo]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.HasSystemAccess.Should().BeTrue();
        response.AppUserId.Should().Be(appUser.Id);
        response.RoleName.Should().Be("Gerente");
        response.ExtraFeatureCount.Should().Be(2);
    }
}
