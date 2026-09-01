using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodLogisticsDeliveryRepository(AppDbContext context) : IIfoodLogisticsDeliveryRepository
{
    public async Task<IfoodLogisticsDelivery?> GetByIfoodOrderIdAsync(long IfoodOrderId, CancellationToken cancellationToken = default)
        => await context.IfoodLogisticsDeliveries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IfoodOrderId == IfoodOrderId, cancellationToken);

    public async Task<IfoodLogisticsDelivery?> GetByIfoodOrderIdForUpdateAsync(long IfoodOrderId, CancellationToken cancellationToken = default)
        => await context.IfoodLogisticsDeliveries
            .FirstOrDefaultAsync(x => x.IfoodOrderId == IfoodOrderId, cancellationToken);

    // "Abertas" é definido pelo status do PEDIDO (IfoodOrder), não da entrega em si — uma entrega
    // com DELIVERY_CODE_VERIFIED continua aparecendo (com selo "Entrega concluída") enquanto o
    // pedido ainda não foi concluído/cancelado no lado do Ifood. Isso evita que a entrega "suma"
    // da tela logo após o código ser verificado e o botão "Atribuir entregador" reaparecer por
    // engano (o handler já bloqueia reatribuição, mas é melhor a tela nem oferecer a ação).
    public async Task<IReadOnlyCollection<IfoodLogisticsDelivery>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodLogisticsDeliveries.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive
                && context.IfoodOrders.Any(io => io.Id == x.IfoodOrderId
                    && io.Status != IfoodOrderStatuses.Concluded && io.Status != IfoodOrderStatuses.Cancelled))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IfoodLogisticsDelivery entity, CancellationToken cancellationToken = default)
        => await context.IfoodLogisticsDeliveries.AddAsync(entity, cancellationToken);
}
