using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IPizzaConfigurationRepository
{
    // Com Sizes/Crusts/Edges/FlavorPrices — para leitura (tela de cardápio, sincronização).
    Task<PizzaConfiguration?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    // Tracked — para AddSize/AddCrust/AddEdge/SetFlavorPrice.
    Task<PizzaConfiguration?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<PizzaConfiguration?> GetByProductIdAsync(long productId, CancellationToken cancellationToken = default);
    Task<PizzaConfiguration?> GetByProductIdForUpdateAsync(long productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PizzaConfiguration>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task AddAsync(PizzaConfiguration entity, CancellationToken cancellationToken = default);
}
