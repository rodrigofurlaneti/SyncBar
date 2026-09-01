using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Role>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe um perfil ativo com este nome na empresa (comparação
    /// case-insensitive — a coluna usa collation utf8mb4_unicode_ci). Usado para impedir
    /// perfis duplicados ao criar um novo (ex.: dois perfis "Garçom").
    /// </summary>
    Task<bool> ExistsByNameAsync(long companyId, string name, CancellationToken cancellationToken = default);

    Task AddAsync(Role entity, CancellationToken cancellationToken = default);
}
