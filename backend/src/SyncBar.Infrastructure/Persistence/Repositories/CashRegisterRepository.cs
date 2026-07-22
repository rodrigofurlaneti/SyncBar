using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CashRegisterRepository(AppDbContext context) : ICashRegisterRepository
{
    public async Task<IReadOnlyCollection<CashRegister>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.CashRegisters.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);
}
