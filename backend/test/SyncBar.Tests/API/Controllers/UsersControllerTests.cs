using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Users;
using SyncBar.Application.Features.Users.Create;
using SyncBar.Application.Features.Users.CreateRole;
using SyncBar.Application.Features.Users.Deactivate;
using SyncBar.Application.Features.Users.GetByCompany;
using SyncBar.Application.Features.Users.GetRoles;
using SyncBar.Application.Features.Users.UpdateRoles;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class UsersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByCompany_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetUsersByCompanyQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<UserResponse>>([]));

        var result = await _controller.GetByCompany(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetUsersByCompanyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<UserResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetByCompany(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRoles_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetRolesQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<RoleResponse>>([]));

        var result = await _controller.GetRoles(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRoles_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetRolesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<RoleResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetRoles(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetByCompany()
    {
        var command = new CreateUserCommand(1, null, "jsilva", "j@x.com", "Senha123!", [1]);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Create(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetByCompany));
        created.RouteValues!["companyId"].Should().Be(1L);
        created.Value.Should().Be(5L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateUserCommand(1, null, "jsilva", "j@x.com", "Senha123!", []);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("AppUser.AlreadyExists", "usuario ja existe")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateRole_Success_ShouldReturnCreatedAtActionPointingToGetRoles()
    {
        var command = new CreateRoleCommand(1, "Caixa", null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(3L));

        var result = await _controller.CreateRole(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetRoles));
        created.RouteValues!["companyId"].Should().Be(1L);
    }

    [Fact]
    public async Task CreateRole_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateRoleCommand(1, "", null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Role.EmptyName", "nome obrigatorio")));

        var result = await _controller.CreateRole(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateRoles_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateUserRolesRequest([1, 2]);
        _mediator.Send(Arg.Is<UpdateUserRolesCommand>(c => c.AppUserId == 1 && c.RoleIds.SequenceEqual(new long[] { 1, 2 })),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.UpdateRoles(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateRoles_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateUserRolesRequest([]);
        _mediator.Send(Arg.Any<UpdateUserRolesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AppUser.NotFound", "usuario nao encontrado")));

        var result = await _controller.UpdateRoles(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Deactivate_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivateUserCommand>(c => c.AppUserId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Deactivate(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AppUser.NotFound", "usuario nao encontrado")));

        var result = await _controller.Deactivate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
