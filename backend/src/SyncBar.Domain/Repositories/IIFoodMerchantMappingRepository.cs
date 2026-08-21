using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodMerchantMappingRepository
{
    Task<IFoodMerchantMapping?> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task<IFoodMerchantMapping?> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default);

    // Lista todos os mapeamentos das filiais de uma empresa (join implícito via Branch) — usado
    // pela tela de Integrações pra mostrar uma linha por loja, mesmo as que ainda não têm
    // MerchantId configurado.
    Task<IReadOnlyDictionary<long, IFoodMerchantMapping>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);

    Task AddAsync(IFoodMerchantMapping entity, CancellationToken cancellationToken = default);
}
