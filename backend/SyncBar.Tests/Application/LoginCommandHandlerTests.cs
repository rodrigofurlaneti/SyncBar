using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Auth.Login;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class LoginCommandHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenProvider _jwtTokenProvider = Substitute.For<IJwtTokenProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private LoginCommandHandler CreateHandler()
        => new(_userRepository, _refreshTokenRepository, _passwordHasher, _jwtTokenProvider, _unitOfWork);

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokens()
    {
        var user = AppUser.Create(1, null, "admin", "admin@bar.com", "hash").Value;
        _userRepository.GetByUserNameForUpdateAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _userRepository.GetRoleNamesAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Administrador" });
        _userRepository.GetPermissionCodesAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Order.Create" });
        _passwordHasher.Verify("secret", "hash").Returns(true);
        _jwtTokenProvider.GenerateToken(user, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new AccessToken("jwt-token", DateTime.UtcNow.AddHours(1)));
        _jwtTokenProvider.GenerateRefreshToken().Returns("refresh-token");

        var result = await CreateHandler().Handle(new LoginCommand("admin", "secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldFailAndRegisterFailure()
    {
        var user = AppUser.Create(1, null, "admin", "admin@bar.com", "hash").Value;
        _userRepository.GetByUserNameForUpdateAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", "hash").Returns(false);

        var result = await CreateHandler().Handle(new LoginCommand("admin", "wrong"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        user.FailedAccessCount.Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ShouldFailWithSameError()
    {
        _userRepository.GetByUserNameForUpdateAsync("ghost", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var result = await CreateHandler().Handle(new LoginCommand("ghost", "any"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }
}
