using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IUnitOfMeasureRepository
{
    // Usada quando um fluxo automatizado (ex.: sincronização de pedidos do iFood) precisa criar um
    // produto placeholder e não tem uma unidade de medida específica para escolher — pega
    // qualquer uma ativa (é um lookup global, não por empresa/filial).
    Task<UnitOfMeasure?> GetFirstActiveAsync(CancellationToken cancellationToken = default);
}
