using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodMerchantMappingRepository
{
    Task<IfoodMerchantMapping?> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task<IfoodMerchantMapping?> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default);

    // Lista todos os mapeamentos das filiais de uma empresa (join implícito via Branch) — usado
    // pela tela de Integrações pra mostrar uma linha por loja, mesmo as que ainda não têm
    // MerchantId configurado.
    Task<IReadOnlyDictionary<long, IfoodMerchantMapping>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);

    Task AddAsync(IfoodMerchantMapping entity, CancellationToken cancellationToken = default);
}
