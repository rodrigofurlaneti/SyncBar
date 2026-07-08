using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Auth.Login;

internal sealed class LoginCommandHandler(
    IAppUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenProvider jwtTokenProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    private static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid user name or password.");

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Tracked — o login atualiza contadores de acesso.
        var user = await userRepository.GetByUserNameForUpdateAsync(request.UserName, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure<LoginResponse>(InvalidCredentials);

        if (user.IsLockedOut())
            return Result.Failure<LoginResponse>(
                new Error("Auth.LockedOut", "Account is temporarily locked. Try again later."));

        // Senha NUNCA verificada em SQL — BCrypt em C#.
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterLoginFailure();
            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Failure<LoginResponse>(InvalidCredentials);
        }

        user.RegisterLoginSuccess();

        var roles = await userRepository.GetRoleNamesAsync(user.Id, cancellationToken);
        var permissions = await userRepository.GetPermissionCodesAsync(user.Id, cancellationToken);
        var accessToken = jwtTokenProvider.GenerateToken(user, roles, permissions);

        var refreshTokenValue = jwtTokenProvider.GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiresAt);
        if (refreshToken.IsFailure)
            return Result.Failure<LoginResponse>(refreshToken.Error);

        await refreshTokenRepository.AddAsync(refreshToken.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new LoginResponse(
            accessToken.Token, accessToken.ExpiresAt,
            refreshTokenValue, refreshTokenExpiresAt,
            user.UserName));
    }
}
