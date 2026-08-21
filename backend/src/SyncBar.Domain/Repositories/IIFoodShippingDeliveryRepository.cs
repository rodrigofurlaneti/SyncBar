using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodShippingDeliveryRepository
{
    Task<IFoodShippingDelivery?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IFoodShippingDelivery?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    // "Abertas" aqui é definido localmente (Status != CANCELLED) — diferente de
    // IIFoodLogisticsDeliveryRepository.GetOpenByBranchAsync, que olha o status de um IFoodOrder
    // vinculado: aqui não há pedido iFood nenhum vinculado, então não há outro sinal de
    // "concluído" pra olhar (o iFood não devolve status de entrega neste módulo).
    Task<IReadOnlyCollection<IFoodShippingDelivery>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IFoodShippingDelivery entity, CancellationToken cancellationToken = default);
}
