using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodIntegrationSettingRepository
{
    Task<IfoodIntegrationSetting?> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<IfoodIntegrationSetting?> GetByCompanyForUpdateAsync(long companyId, CancellationToken cancellationToken = default);
    // Empresas com a integração habilitada — usado pelo IfoodOrderPollingBackgroundService pra
    // saber quais empresas sincronizar a cada ciclo (ignora filtro de tenant de propósito).
    Task<IReadOnlyCollection<long>> GetEnabledCompanyIdsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(IfoodIntegrationSetting entity, CancellationToken cancellationToken = default);
}
