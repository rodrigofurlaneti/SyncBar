using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodLogisticsDeliveryRepository(AppDbContext context) : IIFoodLogisticsDeliveryRepository
{
    public async Task<IFoodLogisticsDelivery?> GetByIFoodOrderIdAsync(long ifoodOrderId, CancellationToken cancellationToken = default)
        => await context.IFoodLogisticsDeliveries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IFoodOrderId == ifoodOrderId, cancellationToken);

    public async Task<IFoodLogisticsDelivery?> GetByIFoodOrderIdForUpdateAsync(long ifoodOrderId, CancellationToken cancellationToken = default)
        => await context.IFoodLogisticsDeliveries
            .FirstOrDefaultAsync(x => x.IFoodOrderId == ifoodOrderId, cancellationToken);

    // "Abertas" é definido pelo status do PEDIDO (IFoodOrder), não da entrega em si — uma entrega
    // com DELIVERY_CODE_VERIFIED continua aparecendo (com selo "Entrega concluída") enquanto o
    // pedido ainda não foi concluído/cancelado no lado do iFood. Isso evita que a entrega "suma"
    // da tela logo após o código ser verificado e o botão "Atribuir entregador" reaparecer por
    // engano (o handler já bloqueia reatribuição, mas é melhor a tela nem oferecer a ação).
    public async Task<IReadOnlyCollection<IFoodLogisticsDelivery>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodLogisticsDeliveries.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive
                && context.IFoodOrders.Any(io => io.Id == x.IFoodOrderId
                    && io.Status != IFoodOrderStatuses.Concluded && io.Status != IFoodOrderStatuses.Cancelled))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodLogisticsDelivery entity, CancellationToken cancellationToken = default)
        => await context.IFoodLogisticsDeliveries.AddAsync(entity, cancellationToken);
}
