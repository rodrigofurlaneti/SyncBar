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
    IAccessLogRepository accessLogRepository,
    ILogTrackerRepository logRepository,
    // ✅ 1. Renomeie o parâmetro aqui para _unitOfWork (com underline)
    IUnitOfWork _unitOfWork)
    // ✅ 2. Passe a variável renomeada para a classe base
    : BaseCommandHandler<LoginCommand, LoginResponse>(logRepository, _unitOfWork)
{
    private static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid user name or password.");

    public override Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(LoginCommandHandler), nameof(Handle), request.IpAddress, async (userIdBox) =>
        {
            var user = await userRepository.GetByUserNameForUpdateAsync(request.UserName, cancellationToken);
            if (user is null || !user.IsActive)
            {
                await LogAsync(null, request, "LoginFailed", cancellationToken);
                return Result.Failure<LoginResponse>(InvalidCredentials);
            }

            userIdBox.Value = user.Id;

            if (user.IsLockedOut())
            {
                await LogAsync(user.Id, request, "Lockout", cancellationToken);
                return Result.Failure<LoginResponse>(
                    new Error("Auth.LockedOut", "Account is temporarily locked. Try again later."));
            }

            if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                user.RegisterLoginFailure();
                await LogAsync(user.Id, request, "LoginFailed", cancellationToken);

                // ✅ 3. Use _unitOfWork aqui normalmente
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Failure<LoginResponse>(InvalidCredentials);
            }

            user.RegisterLoginSuccess();
            await LogAsync(user.Id, request, "Login", cancellationToken);

            var roles = await userRepository.GetRoleNamesAsync(user.Id, cancellationToken);
            var permissions = await userRepository.GetPermissionCodesAsync(user.Id, cancellationToken);
            var accessToken = jwtTokenProvider.GenerateToken(user, roles, permissions);

            var refreshTokenValue = jwtTokenProvider.GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.Now.AddDays(7);
            var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiresAt);
            if (refreshToken.IsFailure)
                return Result.Failure<LoginResponse>(refreshToken.Error);

            await refreshTokenRepository.AddAsync(refreshToken.Value, cancellationToken);

            // ✅ 4. Use _unitOfWork aqui normalmente
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(new LoginResponse(
                accessToken.Token, accessToken.ExpiresAt,
                refreshTokenValue, refreshTokenExpiresAt,
                user.UserName, user.CompanyId, user.EmployeeId));
        });

    private async Task LogAsync(long? userId, LoginCommand request, string eventType, CancellationToken ct)
    {
        var log = Domain.Entities.AccessLog.Create(
            userId, request.UserName, eventType, request.IpAddress, request.UserAgent);
        if (log.IsSuccess)
            await accessLogRepository.AddAsync(log.Value, ct);
    }
}