using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Users.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class CreateUserCommandHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRoleRepository _userRoleRepository = Substitute.For<IUserRoleRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateUserCommandHandler CreateHandler()
        => new(_userRepository, _roleRepository, _userRoleRepository, _passwordHasher, _unitOfWork);

    [Fact]
    public async Task Handle_ShouldHashPasswordAndAssignRoles()
    {
        _userRepository.ExistsAsync("garcom1", "g1@bar.com", Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(Role.Create(1, "Garçom", null).Value);
        _passwordHasher.Hash("Senha@Forte1").Returns("hashed");

        var result = await CreateHandler().Handle(
            new CreateUserCommand(1, 2, "garcom1", "g1@bar.com", "Senha@Forte1", [3]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _passwordHasher.Received(1).Hash("Senha@Forte1");
        await _userRepository.Received(1).AddAsync(
            Arg.Is<AppUser>(u => u.PasswordHash == "hashed"), Arg.Any<CancellationToken>());
        await _userRoleRepository.Received(1).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateUserName_ShouldFail()
    {
        _userRepository.ExistsAsync("garcom1", "g1@bar.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new CreateUserCommand(1, null, "garcom1", "g1@bar.com", "Senha@Forte1", [3]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUser.AlreadyExists");
    }
}
