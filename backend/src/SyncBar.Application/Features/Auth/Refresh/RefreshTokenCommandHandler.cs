using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Auth.Refresh;

internal sealed class RefreshTokenCommandHandler : BaseCommandHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAppUserRepository _userRepository;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAppUserRepository userRepository,
        IJwtTokenProvider jwtTokenProvider,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenProvider = jwtTokenProvider;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(RefreshTokenCommandHandler), nameof(Handle), null, async (userIdBox) =>
        {
            var stored = await _refreshTokenRepository.GetByTokenForUpdateAsync(request.RefreshToken, cancellationToken);
            if (stored is null || !stored.IsValid())
                return Result.Failure<LoginResponse>(
                    new Error("Auth.InvalidRefreshToken", "Refresh token is invalid, expired or revoked."));

            var user = await _userRepository.GetByIdAsync(stored.AppUserId, cancellationToken);
            if (user is null || !user.IsActive)
                return Result.Failure<LoginResponse>(
                    new Error("Auth.InvalidRefreshToken", "Refresh token is invalid, expired or revoked."));

            userIdBox.Value = user.Id;

            stored.Revoke();

            var roles = await _userRepository.GetRoleNamesAsync(user.Id, cancellationToken);
            var permissions = await _userRepository.GetPermissionCodesAsync(user.Id, cancellationToken);
            var accessToken = _jwtTokenProvider.GenerateToken(user, roles, permissions);

            var newTokenValue = _jwtTokenProvider.GenerateRefreshToken();
            var newTokenExpiresAt = DateTime.Now.AddDays(7);
            var newToken = RefreshToken.Create(user.Id, newTokenValue, newTokenExpiresAt);
            if (newToken.IsFailure)
                return Result.Failure<LoginResponse>(newToken.Error);

            await _refreshTokenRepository.AddAsync(newToken.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(new LoginResponse(
                accessToken.Token, accessToken.ExpiresAt,
                newTokenValue, newTokenExpiresAt,
                user.UserName, user.CompanyId, user.EmployeeId));
        });
}