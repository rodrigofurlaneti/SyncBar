using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodFinancialEventRepository
{
    // Dedup por IFoodEventId — a sincronização (Fase 4) roda 1x/dia sobre uma janela de dias que
    // se sobrepõe ao ciclo anterior (evita perder eventos por atraso de apuração do iFood), então
    // precisa checar se o evento já foi gravado antes de inserir de novo.
    Task<bool> ExistsByIFoodEventIdAsync(long branchId, string ifoodEventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IFoodFinancialEvent>> GetByBranchAndPeriodAsync(
        long branchId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    Task AddAsync(IFoodFinancialEvent entity, CancellationToken cancellationToken = default);
}
