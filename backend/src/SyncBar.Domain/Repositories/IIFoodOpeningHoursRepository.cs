using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodOpeningHoursRepository
{
    Task<IReadOnlyCollection<IFoodOpeningHours>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);

    // Tracked — usado pelo handler de salvar pra desativar (soft delete) todos os turnos atuais
    // da filial antes de gravar a lista nova (PUT /opening-hours é sempre substituição total).
    Task<IReadOnlyCollection<IFoodOpeningHours>> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<IFoodOpeningHours> entities, CancellationToken cancellationToken = default);
}
