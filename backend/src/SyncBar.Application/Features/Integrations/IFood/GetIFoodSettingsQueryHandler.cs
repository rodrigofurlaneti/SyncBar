using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood;

internal sealed class GetIFoodSettingsQueryHandler(
    IIFoodIntegrationSettingRepository settingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodSettingsQuery, IFoodSettingsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodSettingsResponse>> Handle(
        GetIFoodSettingsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodSettingsQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var setting = await settingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                if (setting is null)
                    return Result.Success(new IFoodSettingsResponse(false, null, false, null, null, null));

                var hasCredentials = !string.IsNullOrEmpty(setting.ClientId) && !string.IsNullOrEmpty(setting.ClientSecretEncrypted);

                return Result.Success(new IFoodSettingsResponse(
                    HasCredentials: hasCredentials,
                    ClientId: setting.ClientId,
                    Enabled: setting.Enabled,
                    LastConnectionTestAt: setting.LastConnectionTestAt,
                    LastConnectionTestSucceeded: setting.LastConnectionTestSucceeded,
                    IFoodCustomerId: setting.IFoodCustomerId));
            });
    }
}
