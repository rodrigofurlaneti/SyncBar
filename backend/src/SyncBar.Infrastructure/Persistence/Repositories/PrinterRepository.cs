using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class PrinterRepository(AppDbContext context) : IPrinterRepository
{
    public async Task<Printer?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Printers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Printer?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Printers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Printer>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.Printers.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Printer entity, CancellationToken cancellationToken = default)
        => await context.Printers.AddAsync(entity, cancellationToken);
}
