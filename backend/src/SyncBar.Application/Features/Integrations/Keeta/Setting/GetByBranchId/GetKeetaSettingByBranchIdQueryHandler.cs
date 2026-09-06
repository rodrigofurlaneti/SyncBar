using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchId
{
    internal sealed class GetKeetaSettingByBranchIdQueryHandler
        : BaseQueryHandler<GetKeetaSettingByBranchIdQuery, KeetaIntegrationSettingResponse>
    {
        private readonly IKeetaIntegrationSettingRepository _settingRepository;

        public GetKeetaSettingByBranchIdQueryHandler(
            IKeetaIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
        }

        public override async Task<Result<KeetaIntegrationSettingResponse>> Handle(
            GetKeetaSettingByBranchIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaSettingByBranchIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);

                    if (setting is null)
                    {
                        return Result.Failure<KeetaIntegrationSettingResponse>(
                            Error.NotFound(
                                "KeetaSetting.NotFound",
                                $"Configuração de integração Keeta para a filial {request.BranchId} não foi encontrada."));
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
