using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Users.Deactivate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Users.Deactivate;

public sealed class DeactivateUserCommandHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivateUserCommandHandler _handler;

    public DeactivateUserCommandHandlerTests()
    {
        _handler = new DeactivateUserCommandHandler(_userRepository, _logRepository, _unitOfWork);
    }

    private static AppUser CreateActiveUser()
        => AppUser.Create(companyId: 1, employeeId: null, userName: "usuario.teste", email: "usuario@teste.com", passwordHash: "hash-fake").Value;

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var command = new DeactivateUserCommand(AppUserId: 1);
        _userRepository.GetByIdForUpdateAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUser.NotFound");
        // Nenhum commit explícito do handler; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserAlreadyInactive_ShouldReturnFailureWithoutCommitting()
    {
        var user = CreateActiveUser();
        user.Deactivate(); // já desativado antes deste Handle
        var command = new DeactivateUserCommand(AppUserId: 1);
        _userRepository.GetByIdForUpdateAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUser.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeactivateUserAndCommit()
    {
        var user = CreateActiveUser();
        var command = new DeactivateUserCommand(AppUserId: 1);
        _userRepository.GetByIdForUpdateAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
