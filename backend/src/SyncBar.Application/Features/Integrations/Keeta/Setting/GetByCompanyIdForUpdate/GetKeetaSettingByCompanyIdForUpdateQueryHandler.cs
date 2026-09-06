using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByCompanyIdForUpdate
{
    internal sealed class GetKeetaSettingByCompanyIdForUpdateQueryHandler
        : BaseQueryHandler<GetKeetaSettingByCompanyIdForUpdateQuery, KeetaIntegrationSettingResponse>
    {
        private readonly IKeetaIntegrationSettingRepository _settingRepository;

        public GetKeetaSettingByCompanyIdForUpdateQueryHandler(
            IKeetaIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<KeetaIntegrationSettingResponse>> Handle(
            GetKeetaSettingByCompanyIdForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaSettingByCompanyIdForUpdateQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByCompanyIdForUpdateAsync(request.CompanyId, cancellationToken);

                    if (setting is null)
                    {
                        return Result.Failure<KeetaIntegrationSettingResponse>(
                            Error.NotFound(
                                "KeetaSetting.NotFound",
                                $"Configuração de integração Keeta para a empresa {request.CompanyId} não foi encontrada."));
                    }

                    var response = new KeetaIntegrationSettingResponse(
                        setting.Id,
                        setting.CompanyId,
                        setting.BranchId,
                        setting.ClientId,
                        setting.AppId,
                        setting.BaseUrl,
                        !string.IsNullOrWhiteSpace(setting.CurrentAccessToken),
                        setting.TokenExpiresAtUtc,
                        setting.UpdatedAtUtc);

                    return Result.Success(response);
                });
        }
    }
}
