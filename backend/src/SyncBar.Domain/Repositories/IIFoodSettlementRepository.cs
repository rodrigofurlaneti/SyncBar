using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodSettlementRepository
{
    // Diferente de FinancialEvent (dedup só), Settlement precisa de get-or-update: o mesmo
    // título pode ser retornado de novo pela API com status diferente (ex.: PENDING → SUCCEED)
    // conforme o Ifood processa o repasse — a sincronização atualiza em vez de duplicar.
    Task<IfoodSettlement?> GetByIfoodSettlementIdForUpdateAsync(long branchId, string IfoodSettlementId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IfoodSettlement>> GetByBranchAndPeriodAsync(
        long branchId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    Task AddAsync(IfoodSettlement entity, CancellationToken cancellationToken = default);
}
