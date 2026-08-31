using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Auth;
using SyncBar.Application.Features.Auth.Login;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Auth.Login;

public sealed class LoginCommandHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenProvider _jwtTokenProvider = Substitute.For<IJwtTokenProvider>();
    private readonly IAccessLogRepository _accessLogRepository = Substitute.For<IAccessLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepository,
            _refreshTokenRepository,
            _passwordHasher,
            _jwtTokenProvider,
            _accessLogRepository,
            _logRepository,
            _unitOfWork);
    }

    private static AppUser CreateActiveUser(
        long companyId = 1,
        long? employeeId = null,
        string userName = "waiter",
        string passwordHash = "hashed-password")
        => AppUser.Create(companyId, employeeId, userName, $"{userName}@bar.com", passwordHash).Value;

    [Fact]
    public async Task Handle_UserDoesNotExist_ShouldReturnInvalidCredentialsFailureAndLogFailedAttempt()
    {
        var command = new LoginCommand("unknown", "any-password");
        _userRepository.GetByUserNameForUpdateAsync(command.UserName, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");

        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "LoginFailed" && l.AppUserId == null),
            Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        // Sem commit explícito nesse ramo do handler: o único commit vem do finally da base (log de auditoria).
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserIsInactive_ShouldReturnInvalidCredentialsFailure()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var command = new LoginCommand(user.UserName, "any-password");
        _userRepository.GetByUserNameForUpdateAsync(command.UserName, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");

        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "LoginFailed" && l.AppUserId == null),
            Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_UserIsLockedOut_ShouldReturnLockedOutFailure()
    {
        var user = CreateActiveUser();
        for (var i = 0; i < 5; i++)
            user.RegisterLoginFailure();
        user.IsLockedOut().Should().BeTrue();

        var command = new LoginCommand(user.UserName, "any-password");
        _userRepository.GetByUserNameForUpdateAsync(command.UserName, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.LockedOut");

        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "Lockout" && l.AppUserId == user.Id),
            Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
        // Sem commit explícito nesse ramo: só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldRegisterFailureLogAndReturnInvalidCredentials()
    {
        var user = CreateActiveUser();
        var command = new LoginCommand(user.UserName, "wrong-password");
        _userRepository.GetByUserNameForUpdateAsync(command.UserName, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        user.FailedAccessCount.Should().Be(1);

        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "LoginFailed" && l.AppUserId == user.Id),
            Arg.Any<CancellationToken>());
        _jwtTokenProvider.DidNotReceive().GenerateToken(
            Arg.Any<AppUser>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>());
        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        // Commit explícito do handler no ramo de senha errada + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnSuccessAndPersistRefreshToken()
    {
        var user = CreateActiveUser(companyId: 7, employeeId: 42, userName: "manager");
        var command = new LoginCommand(user.UserName, "correct-password", "127.0.0.1", "test-agent");

        _userRepository.GetByUserNameForUpdateAsync(command.UserName, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);

        var roles = new[] { "Manager" };
        var permissions = new[] { "orders.read" };
        _userRepository.GetRoleNamesAsync(user.Id, Arg.Any<CancellationToken>()).Returns(roles);
        _userRepository.GetPermissionCodesAsync(user.Id, Arg.Any<CancellationToken>()).Returns(permissions);

        var accessToken = new AccessToken("access-token-value", DateTime.Now.AddHours(1));
        _jwtTokenProvider.GenerateToken(user, roles, permissions).Returns(accessToken);
        _jwtTokenProvider.GenerateRefreshToken().Returns("refresh-token-value");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token-value");
        result.Value.AccessTokenExpiresAt.Should().Be(accessToken.ExpiresAt);
        result.Value.RefreshToken.Should().Be("refresh-token-value");
        result.Value.UserName.Should().Be(user.UserName);
        result.Value.CompanyId.Should().Be(user.CompanyId);
        result.Value.EmployeeId.Should().Be(user.EmployeeId);

        user.FailedAccessCount.Should().Be(0);
        user.LastLoginAt.Should().NotBeNull();

        await _refreshTokenRepository.Received(1).AddAsync(
            Arg.Is<RefreshToken>(rt => rt.Token == "refresh-token-value" && rt.AppUserId == user.Id),
            Arg.Any<CancellationToken>());
        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "Login" && l.AppUserId == user.Id),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo de sucesso + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RefreshTokenCreationFails_ShouldReturnFailureWithoutPersistingRefreshToken()
    {
        var user = CreateActiveUser();
        var command = new LoginCommand(user.UserName, "correct-password");

        _userRepository.GetByUserNameForUpdateAsync(command.UserName, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);
        _userRepository.GetRoleNamesAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());
        _userRepository.GetPermissionCodesAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());
        _jwtTokenProvider.GenerateToken(user, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new AccessToken("access-token-value", DateTime.Now.AddHours(1)));
        // Token vazio faz RefreshToken.Create falhar (Error "RefreshToken.EmptyToken").
        _jwtTokenProvider.GenerateRefreshToken().Returns(string.Empty);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RefreshToken.EmptyToken");

        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
