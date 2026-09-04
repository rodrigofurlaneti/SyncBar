using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Auth.CustomerLogin;

internal sealed class CustomerLoginCommandHandler : BaseCommandHandler<CustomerLoginCommand, CustomerLoginResponse>
{
    private readonly ICustomerAppUserRepository _customerUserRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    private readonly IAccessLogRepository _accessLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "E-mail ou senha incorretos.");

    public CustomerLoginCommandHandler(
        ICustomerAppUserRepository customerUserRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenProvider jwtTokenProvider,
        IAccessLogRepository accessLogRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _customerUserRepository = customerUserRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenProvider = jwtTokenProvider;
        _accessLogRepository = accessLogRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<CustomerLoginResponse>> Handle(CustomerLoginCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(CustomerLoginCommandHandler), nameof(Handle), request.IpAddress, async (userIdBox) =>
        {
            var customer = await _customerUserRepository.GetByEmailForUpdateAsync(request.Email, request.CompanyId, cancellationToken);

            if (customer is null || !customer.IsActive)
            {
                // Alterado para "LoginFailed" para respeitar a Check Constraint do banco
                await LogAsync(null, request, "LoginFailed", cancellationToken);
                return Result.Failure<CustomerLoginResponse>(InvalidCredentials);
            }

            userIdBox.Value = customer.Id;

            if (!_passwordHasher.Verify(request.Password, customer.PasswordHash))
            {
                // Alterado para "LoginFailed" para respeitar a Check Constraint do banco
                await LogAsync(customer.Id, request, "LoginFailed", cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Failure<CustomerLoginResponse>(InvalidCredentials);
            }

            // Certifique-se de que a entidade CustomerAppUser possui este método, ou remova a linha abaixo
            customer.RegisterLoginSuccess();

            // Alterado para "Login" para respeitar a Check Constraint do banco
            await LogAsync(customer.Id, request, "Login", cancellationToken);

            var roles = new List<string> { "Customer" };
            var permissions = new List<string>();

            var accessToken = _jwtTokenProvider.GenerateCustomerToken(customer, roles, permissions);

            var refreshTokenValue = _jwtTokenProvider.GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.Now.AddDays(7);
            var refreshToken = RefreshToken.Create(customer.Id, refreshTokenValue, refreshTokenExpiresAt);

            if (refreshToken.IsFailure)
                return Result.Failure<CustomerLoginResponse>(refreshToken.Error);

            await _refreshTokenRepository.AddAsync(refreshToken.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(new CustomerLoginResponse(
                accessToken.Token,
                accessToken.ExpiresAt,
                refreshTokenValue,
                refreshTokenExpiresAt,
                customer.UserName,
                customer.Id,
                customer.CompanyId));
        });

    private async Task LogAsync(long? userId, CustomerLoginCommand request, string eventType, CancellationToken ct)
    {
        var log = Domain.Entities.AccessLog.Create(
            userId, request.Email, eventType, request.IpAddress, request.UserAgent);

        if (log.IsSuccess)
            await _accessLogRepository.AddAsync(log.Value, ct);
    }
}