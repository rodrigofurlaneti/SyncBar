using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodSettlementRepository
{
    // Diferente de FinancialEvent (dedup só), Settlement precisa de get-or-update: o mesmo
    // título pode ser retornado de novo pela API com status diferente (ex.: PENDING → SUCCEED)
    // conforme o iFood processa o repasse — a sincronização atualiza em vez de duplicar.
    Task<IFoodSettlement?> GetByIFoodSettlementIdForUpdateAsync(long branchId, string ifoodSettlementId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IFoodSettlement>> GetByBranchAndPeriodAsync(
        long branchId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    Task AddAsync(IFoodSettlement entity, CancellationToken cancellationToken = default);
}
