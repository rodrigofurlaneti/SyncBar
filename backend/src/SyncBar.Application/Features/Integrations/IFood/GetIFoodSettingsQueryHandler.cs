using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood;

internal sealed class GetIfoodSettingsQueryHandler(
    IIfoodIntegrationSettingRepository settingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodSettingsQuery, IfoodSettingsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodSettingsResponse>> Handle(
        GetIfoodSettingsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodSettingsQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var setting = await settingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                if (setting is null)
                    return Result.Success(new IfoodSettingsResponse(false, null, false, null, null, null));

                var hasCredentials = !string.IsNullOrEmpty(setting.ClientId) && !string.IsNullOrEmpty(setting.ClientSecretEncrypted);

                return Result.Success(new IfoodSettingsResponse(
                    HasCredentials: hasCredentials,
                    ClientId: setting.ClientId,
                    Enabled: setting.Enabled,
                    LastConnectionTestAt: setting.LastConnectionTestAt,
                    LastConnectionTestSucceeded: setting.LastConnectionTestSucceeded,
                    IfoodCustomerId: setting.IfoodCustomerId));
            });
    }
}
