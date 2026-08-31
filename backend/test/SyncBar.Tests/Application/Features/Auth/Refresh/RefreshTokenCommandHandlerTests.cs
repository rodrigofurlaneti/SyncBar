using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Auth;
using SyncBar.Application.Features.Auth.Refresh;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Auth.Refresh;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IJwtTokenProvider _jwtTokenProvider = Substitute.For<IJwtTokenProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepository,
            _userRepository,
            _jwtTokenProvider,
            _logRepository,
            _unitOfWork);
    }

    private static AppUser CreateActiveUser(
        long companyId = 1,
        long? employeeId = null,
        string userName = "waiter",
        string passwordHash = "hashed-password")
        => AppUser.Create(companyId, employeeId, userName, $"{userName}@bar.com", passwordHash).Value;

    private static RefreshToken CreateValidStoredToken(long appUserId, string token = "old-refresh-token-value")
        => RefreshToken.Create(appUserId, token, DateTime.Now.AddDays(3)).Value;

    [Fact]
    public async Task Handle_TokenNotFound_ShouldReturnInvalidRefreshTokenFailure()
    {
        var command = new RefreshTokenCommand("unknown-token");
        _refreshTokenRepository.GetByTokenForUpdateAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");

        await _userRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        // Sem commit explícito nesse ramo do handler: o único commit vem do finally da base (log de auditoria).
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StoredTokenIsRevoked_ShouldReturnInvalidRefreshTokenFailure()
    {
        var user = CreateActiveUser();
        var stored = CreateValidStoredToken(user.Id);
        stored.Revoke();

        var command = new RefreshTokenCommand(stored.Token);
        _refreshTokenRepository.GetByTokenForUpdateAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(stored);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");

        await _userRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnInvalidRefreshTokenFailure()
    {
        var stored = CreateValidStoredToken(appUserId: 1);
        var command = new RefreshTokenCommand(stored.Token);
        _refreshTokenRepository.GetByTokenForUpdateAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(stored);
        _userRepository.GetByIdAsync(stored.AppUserId, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");

        _jwtTokenProvider.DidNotReceive().GenerateToken(
            Arg.Any<AppUser>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>());
        stored.RevokedAt.Should().BeNull();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserIsInactive_ShouldReturnInvalidRefreshTokenFailure()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var stored = CreateValidStoredToken(user.Id);
        var command = new RefreshTokenCommand(stored.Token);
        _refreshTokenRepository.GetByTokenForUpdateAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(stored);
        _userRepository.GetByIdAsync(stored.AppUserId, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
        stored.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldRevokeOldTokenAndReturnNewTokens()
    {
        var user = CreateActiveUser(companyId: 7, employeeId: 42, userName: "manager");
        var stored = CreateValidStoredToken(user.Id);
        var command = new RefreshTokenCommand(stored.Token);

        _refreshTokenRepository.GetByTokenForUpdateAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(stored);
        _userRepository.GetByIdAsync(stored.AppUserId, Arg.Any<CancellationToken>())
            .Returns(user);

        var roles = new[] { "Manager" };
        var permissions = new[] { "orders.read" };
        _userRepository.GetRoleNamesAsync(user.Id, Arg.Any<CancellationToken>()).Returns(roles);
        _userRepository.GetPermissionCodesAsync(user.Id, Arg.Any<CancellationToken>()).Returns(permissions);

        var accessToken = new AccessToken("access-token-value", DateTime.Now.AddHours(1));
        _jwtTokenProvider.GenerateToken(user, roles, permissions).Returns(accessToken);
        _jwtTokenProvider.GenerateRefreshToken().Returns("new-refresh-token-value");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token-value");
        result.Value.AccessTokenExpiresAt.Should().Be(accessToken.ExpiresAt);
        result.Value.RefreshToken.Should().Be("new-refresh-token-value");
        result.Value.UserName.Should().Be(user.UserName);
        result.Value.CompanyId.Should().Be(user.CompanyId);
        result.Value.EmployeeId.Should().Be(user.EmployeeId);

        stored.RevokedAt.Should().NotBeNull();
        await _refreshTokenRepository.Received(1).AddAsync(
            Arg.Is<RefreshToken>(rt => rt.Token == "new-refresh-token-value" && rt.AppUserId == user.Id),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo de sucesso + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewRefreshTokenCreationFails_ShouldReturnFailureWithoutPersistingNewToken()
    {
        var user = CreateActiveUser();
        var stored = CreateValidStoredToken(user.Id);
        var command = new RefreshTokenCommand(stored.Token);

        _refreshTokenRepository.GetByTokenForUpdateAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(stored);
        _userRepository.GetByIdAsync(stored.AppUserId, Arg.Any<CancellationToken>())
            .Returns(user);
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
        // O handler já havia revogado o token antigo antes de tentar criar o novo.
        stored.RevokedAt.Should().NotBeNull();
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
