using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.RefreshAccessToken
{
    internal sealed class RefreshKeetaAccessTokenCommandHandler
        : BaseCommandHandler<RefreshKeetaAccessTokenCommand, RefreshKeetaAccessTokenResponse>
    {
        private readonly IKeetaAccessTokenProvider _tokenProvider;
        private readonly IKeetaIntegrationSettingRepository _settingRepository;

        public RefreshKeetaAccessTokenCommandHandler(
            IKeetaAccessTokenProvider tokenProvider,
            IKeetaIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _tokenProvider = tokenProvider;
            _settingRepository = settingRepository;
        }

        public override async Task<Result<RefreshKeetaAccessTokenResponse>> Handle(
            RefreshKeetaAccessTokenCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RefreshKeetaAccessTokenCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var tokenResult = await _tokenProvider.GetValidAccessTokenAsync(
                        request.CompanyId, request.BranchId, forceRefresh: true, cancellationToken: cancellationToken);

                    if (tokenResult.IsFailure)
                        return Result.Failure<RefreshKeetaAccessTokenResponse>(tokenResult.Error);

                    var setting = await _settingRepository.GetByBranchOrCompanyFallbackAsync(request.CompanyId, request.BranchId, cancellationToken);

                    return Result.Success(new RefreshKeetaAccessTokenResponse(setting?.TokenExpiresAtUtc));
                });
        }
    }
}
