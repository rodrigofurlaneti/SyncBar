using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Auth.Login;

internal sealed class LoginCommandHandler : BaseCommandHandler<LoginCommand, LoginResponse>
{
    private readonly IAppUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    private readonly IAccessLogRepository _accessLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid user name or password.");

    // ✅ Construtor tradicional substitui o construtor primário
    public LoginCommandHandler(
        IAppUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenProvider jwtTokenProvider,
        IAccessLogRepository accessLogRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork) // Passa para a base
    {
        // Atribui os campos locais
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenProvider = jwtTokenProvider;
        _accessLogRepository = accessLogRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(LoginCommandHandler), nameof(Handle), request.IpAddress, async (userIdBox) =>
        {
            var user = await _userRepository.GetByUserNameForUpdateAsync(request.UserName, cancellationToken);
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

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                user.RegisterLoginFailure();
                await LogAsync(user.Id, request, "LoginFailed", cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Failure<LoginResponse>(InvalidCredentials);
            }

            user.RegisterLoginSuccess();
            await LogAsync(user.Id, request, "Login", cancellationToken);

            var roles = await _userRepository.GetRoleNamesAsync(user.Id, cancellationToken);
            var permissions = await _userRepository.GetPermissionCodesAsync(user.Id, cancellationToken);
            var accessToken = _jwtTokenProvider.GenerateToken(user, roles, permissions);

            var refreshTokenValue = _jwtTokenProvider.GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.Now.AddDays(7);
            var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiresAt);
            if (refreshToken.IsFailure)
                return Result.Failure<LoginResponse>(refreshToken.Error);

            await _refreshTokenRepository.AddAsync(refreshToken.Value, cancellationToken);

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
            await _accessLogRepository.AddAsync(log.Value, ct);
    }
}