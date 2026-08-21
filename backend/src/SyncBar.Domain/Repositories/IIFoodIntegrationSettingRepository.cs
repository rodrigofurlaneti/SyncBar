using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodIntegrationSettingRepository
{
    Task<IFoodIntegrationSetting?> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<IFoodIntegrationSetting?> GetByCompanyForUpdateAsync(long companyId, CancellationToken cancellationToken = default);
    // Empresas com a integração habilitada — usado pelo IFoodOrderPollingBackgroundService pra
    // saber quais empresas sincronizar a cada ciclo (ignora filtro de tenant de propósito).
    Task<IReadOnlyCollection<long>> GetEnabledCompanyIdsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(IFoodIntegrationSetting entity, CancellationToken cancellationToken = default);
}
