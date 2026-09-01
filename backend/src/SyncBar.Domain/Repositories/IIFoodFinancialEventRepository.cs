using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodFinancialEventRepository
{
    // Dedup por IfoodEventId — a sincronização (Fase 4) roda 1x/dia sobre uma janela de dias que
    // se sobrepõe ao ciclo anterior (evita perder eventos por atraso de apuração do Ifood), então
    // precisa checar se o evento já foi gravado antes de inserir de novo.
    Task<bool> ExistsByIfoodEventIdAsync(long branchId, string IfoodEventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IfoodFinancialEvent>> GetByBranchAndPeriodAsync(
        long branchId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    Task AddAsync(IfoodFinancialEvent entity, CancellationToken cancellationToken = default);
}
