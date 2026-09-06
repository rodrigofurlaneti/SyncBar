using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByScope
{
    internal sealed class GetKeetaSettingByScopeQueryHandler
        : BaseQueryHandler<GetKeetaSettingByScopeQuery, KeetaIntegrationSettingResponse>
    {
        private readonly IKeetaIntegrationSettingRepository _settingRepository;

        public GetKeetaSettingByScopeQueryHandler(
            IKeetaIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<KeetaIntegrationSettingResponse>> Handle(
            GetKeetaSettingByScopeQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaSettingByScopeQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByScopeAsync(
                        request.CompanyId, request.BranchId, cancellationToken);

                    if (setting is null)
                    {
                        return Result.Failure<KeetaIntegrationSettingResponse>(
                            Error.NotFound(
                                "KeetaSetting.NotFound",
                                $"Nenhuma configuração de integração Keeta encontrada para o escopo informado."));
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
