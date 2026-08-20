using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodLogisticsDeliveryRepository
{
    // ifoodOrderId aqui é o Id LOCAL (long) do IFoodOrder — não a string do iFood.
    Task<IFoodLogisticsDelivery?> GetByIFoodOrderIdAsync(long ifoodOrderId, CancellationToken cancellationToken = default);
    Task<IFoodLogisticsDelivery?> GetByIFoodOrderIdForUpdateAsync(long ifoodOrderId, CancellationToken cancellationToken = default);
    // "Abertas" = o PEDIDO (IFoodOrder) associado ainda não foi concluído/cancelado no iFood —
    // não o status da própria entrega, pra continuar mostrando entregas já com código verificado
    // enquanto o pedido segue aberto (ver comentário na implementação). Para a tela "Pedidos
    // iFood"/"Logística" (fase 7).
    Task<IReadOnlyCollection<IFoodLogisticsDelivery>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IFoodLogisticsDelivery entity, CancellationToken cancellationToken = default);
}
