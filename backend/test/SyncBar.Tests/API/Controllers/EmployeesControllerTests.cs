using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Employees;
using SyncBar.Application.Features.Employees.Create;
using SyncBar.Application.Features.Employees.CreateJobTitle;
using SyncBar.Application.Features.Employees.Dismiss;
using SyncBar.Application.Features.Employees.GetByBranch;
using SyncBar.Application.Features.Employees.GetJobTitles;
using SyncBar.Application.Features.Employees.RegisterTeamMember;
using SyncBar.Application.Features.Employees.SetCommission;
using SyncBar.Application.Features.Employees.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class EmployeesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        _controller = new EmployeesController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetEmployeesByBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<EmployeeResponse>>([]));

        var result = await _controller.GetByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetEmployeesByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<EmployeeResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranch(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetJobTitles_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetJobTitlesQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<JobTitleResponse>>([]));

        var result = await _controller.GetJobTitles(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetJobTitles_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetJobTitlesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<JobTitleResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetJobTitles(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateJobTitle_Success_ShouldReturnCreatedAtActionPointingToGetJobTitles()
    {
        var command = new CreateJobTitleCommand(1, "Garcom");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(3L));

        var result = await _controller.CreateJobTitle(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetJobTitles));
        created.RouteValues!["companyId"].Should().Be(1L);
    }

    [Fact]
    public async Task CreateJobTitle_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateJobTitleCommand(1, "");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("JobTitle.EmptyName", "nome obrigatorio")));

        var result = await _controller.CreateJobTitle(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateEmployeeCommand ValidCreateCommand() => new(1, 1, "Joao Silva", "12345678900", null, null, DateTime.Today, null);

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetByBranch()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(4L));

        var result = await _controller.Create(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetByBranch));
        created.RouteValues!["branchId"].Should().Be(1L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("JobTitle.NotFound", "cargo nao encontrado")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static RegisterTeamMemberCommand ValidTeamMemberCommand() => new(
        1, 1, 1, "Joao Silva", "12345678900", null, null, DateTime.Today, null, false, null, null, null, null);

    [Fact]
    public async Task RegisterTeamMember_Success_ShouldReturnCreatedAtActionPointingToGetByBranch()
    {
        var command = ValidTeamMemberCommand();
        var response = new RegisterTeamMemberResult(5L, null, null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.RegisterTeamMember(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetByBranch));
        created.RouteValues!["branchId"].Should().Be(1L);
        created.Value.Should().Be(response);
    }

    [Fact]
    public async Task RegisterTeamMember_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidTeamMemberCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RegisterTeamMemberResult>(new Error("JobTitle.NotFound", "cargo nao encontrado")));

        var result = await _controller.RegisterTeamMember(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateEmployeeRequest(1, "Joao Silva", null, null, null);
        _mediator.Send(Arg.Is<UpdateEmployeeCommand>(c => c.EmployeeId == 1 && c.JobTitleId == 1 && c.Name == "Joao Silva"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateEmployeeRequest(1, "Joao Silva", null, null, null);
        _mediator.Send(Arg.Any<UpdateEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Employee.NotFound", "funcionario nao encontrado")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Dismiss_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DismissEmployeeCommand>(c => c.EmployeeId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Dismiss(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Dismiss_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DismissEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Employee.NotFound", "funcionario nao encontrado")));

        var result = await _controller.Dismiss(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetCommission_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new SetCommissionRequest(0.05m);
        _mediator.Send(Arg.Is<SetCommissionCommand>(c => c.EmployeeId == 1 && c.CommissionPercent == 0.05m), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.SetCommission(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetCommission_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SetCommissionRequest(null);
        _mediator.Send(Arg.Any<SetCommissionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Employee.NotFound", "funcionario nao encontrado")));

        var result = await _controller.SetCommission(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
