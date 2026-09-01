using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Igual a GetByCompanyAsync, mas SEM o filtro IsActive — inclui categorias desativadas.
    /// Uso restrito à tela de gerenciamento (Cardápio admin); telas de pedido/venda devem
    /// continuar usando GetByCompanyAsync para nunca oferecer uma categoria desativada.
    /// </summary>
    Task<IReadOnlyCollection<Category>> GetAllByCompanyAsync(long companyId, CancellationToken cancellationToken = default);

    Task AddAsync(Category entity, CancellationToken cancellationToken = default);
}
