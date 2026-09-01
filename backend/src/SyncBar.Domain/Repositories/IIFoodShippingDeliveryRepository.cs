using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodShippingDeliveryRepository
{
    Task<IfoodShippingDelivery?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IfoodShippingDelivery?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    // "Abertas" aqui é definido localmente (Status != CANCELLED) — diferente de
    // IIfoodLogisticsDeliveryRepository.GetOpenByBranchAsync, que olha o status de um IfoodOrder
    // vinculado: aqui não há pedido Ifood nenhum vinculado, então não há outro sinal de
    // "concluído" pra olhar (o Ifood não devolve status de entrega neste módulo).
    Task<IReadOnlyCollection<IfoodShippingDelivery>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IfoodShippingDelivery entity, CancellationToken cancellationToken = default);
}
