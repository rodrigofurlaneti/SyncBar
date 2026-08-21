using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IComplementGroupRepository
{
    // Com Complements — para leitura (tela de detalhe, sincronização de catálogo).
    Task<ComplementGroup?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    // Tracked, com Complements — para AddComplement/RemoveComplement/UpdateDetails.
    Task<ComplementGroup?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ComplementGroup>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ComplementGroup>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task AddAsync(ComplementGroup entity, CancellationToken cancellationToken = default);
}
