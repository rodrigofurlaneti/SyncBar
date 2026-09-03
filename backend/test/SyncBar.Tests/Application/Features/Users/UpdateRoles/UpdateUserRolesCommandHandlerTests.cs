using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Users.UpdateRoles;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Users.UpdateRoles;

public sealed class UpdateUserRolesCommandHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRoleRepository _userRoleRepository = Substitute.For<IUserRoleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateUserRolesCommandHandler _handler;

    public UpdateUserRolesCommandHandlerTests()
    {
        _handler = new UpdateUserRolesCommandHandler(
            _userRepository, _roleRepository, _userRoleRepository, _logRepository, _unitOfWork);
    }

    private static AppUser CreateActiveUser(long companyId = 1)
        => AppUser.Create(companyId, employeeId: null, userName: "usuario.teste", email: "usuario@teste.com", passwordHash: "hash-fake").Value;

    private static Role CreateActiveRole(long companyId = 1, string name = "Gerente")
        => Role.Create(companyId, name, null).Value;

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var command = new UpdateUserRolesCommand(AppUserId: 1, RoleIds: [1]);
        _userRepository.GetByIdAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUser.NotFound");
        // Nenhum commit explícito do handler; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserInactive_ShouldReturnFailureWithoutCommitting()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var command = new UpdateUserRolesCommand(AppUserId: 1, RoleIds: [1]);
        _userRepository.GetByIdAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUser.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DesiredRoleNotFoundForCompany_ShouldReturnFailureWithoutCommitting()
    {
        var user = CreateActiveUser();
        var command = new UpdateUserRolesCommand(AppUserId: 1, RoleIds: [1, 2]);
        _userRepository.GetByIdAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepository.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Role.NotFound");
        await _userRoleRepository.DidNotReceive().GetByUserForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeactivateRemovedRolesAndAddNewOnes()
    {
        var user = CreateActiveUser();
        var command = new UpdateUserRolesCommand(AppUserId: 1, RoleIds: [2, 3]);
        _userRepository.GetByIdAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _roleRepository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());

        // Vínculos atuais: role 1 (será removido/desativado) e role 2 (permanece, não recriar).
        var linkRole1 = UserRole.Create(user.CompanyId, user.Id, roleId: 1).Value;
        var linkRole2 = UserRole.Create(user.CompanyId, user.Id, roleId: 2).Value;
        _userRoleRepository.GetByUserForUpdateAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([linkRole1, linkRole2]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        linkRole1.IsActive.Should().BeFalse("role 1 não está mais na lista desejada");
        linkRole2.IsActive.Should().BeTrue("role 2 continua desejado, o vínculo existente é reaproveitado");

        // Só a role 3 é nova — role 2 já tinha vínculo ativo, não deve ser duplicado.
        await _userRoleRepository.Received(1).AddAsync(
            Arg.Is<UserRole>(l => l.RoleId == 3 && l.AppUserId == user.Id),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithNoCurrentLinks_ShouldAddAllDesiredRoles()
    {
        var user = CreateActiveUser();
        var command = new UpdateUserRolesCommand(AppUserId: 1, RoleIds: [1, 2]);
        _userRepository.GetByIdAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _roleRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _userRoleRepository.GetByUserForUpdateAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserRole>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _userRoleRepository.Received(2).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
