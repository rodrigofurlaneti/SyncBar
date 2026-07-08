using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Auth.Refresh;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IAppUserRepository userRepository,
    IJwtTokenProvider jwtTokenProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RefreshTokenCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var stored = await refreshTokenRepository.GetByTokenForUpdateAsync(request.RefreshToken, cancellationToken);
        if (stored is null || !stored.IsValid())
            return Result.Failure<LoginResponse>(
                new Error("Auth.InvalidRefreshToken", "Refresh token is invalid, expired or revoked."));

        var user = await userRepository.GetByIdAsync(stored.AppUserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure<LoginResponse>(
                new Error("Auth.InvalidRefreshToken", "Refresh token is invalid, expired or revoked."));

        // Rotacao: revoga o antigo e emite um novo.
        stored.Revoke();

        var roles = await userRepository.GetRoleNamesAsync(user.Id, cancellationToken);
        var permissions = await userRepository.GetPermissionCodesAsync(user.Id, cancellationToken);
        var accessToken = jwtTokenProvider.GenerateToken(user, roles, permissions);

        var newTokenValue = jwtTokenProvider.GenerateRefreshToken();
        var newTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        var newToken = Domain.Entities.RefreshToken.Create(user.Id, newTokenValue, newTokenExpiresAt);
        if (newToken.IsFailure)
            return Result.Failure<LoginResponse>(newToken.Error);

        await refreshTokenRepository.AddAsync(newToken.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new LoginResponse(
            accessToken.Token, accessToken.ExpiresAt,
            newTokenValue, newTokenExpiresAt,
            user.UserName));
    }
}
