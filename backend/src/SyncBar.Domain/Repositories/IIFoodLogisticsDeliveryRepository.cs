using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodLogisticsDeliveryRepository
{
    // IfoodOrderId aqui é o Id LOCAL (long) do IfoodOrder — não a string do Ifood.
    Task<IfoodLogisticsDelivery?> GetByIfoodOrderIdAsync(long IfoodOrderId, CancellationToken cancellationToken = default);
    Task<IfoodLogisticsDelivery?> GetByIfoodOrderIdForUpdateAsync(long IfoodOrderId, CancellationToken cancellationToken = default);
    // "Abertas" = o PEDIDO (IfoodOrder) associado ainda não foi concluído/cancelado no Ifood —
    // não o status da própria entrega, pra continuar mostrando entregas já com código verificado
    // enquanto o pedido segue aberto (ver comentário na implementação). Para a tela "Pedidos
    // Ifood"/"Logística" (fase 7).
    Task<IReadOnlyCollection<IfoodLogisticsDelivery>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IfoodLogisticsDelivery entity, CancellationToken cancellationToken = default);
}
