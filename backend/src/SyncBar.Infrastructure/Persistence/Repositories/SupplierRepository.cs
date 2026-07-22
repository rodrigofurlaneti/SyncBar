using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class SupplierRepository(AppDbContext context) : ISupplierRepository
{
    public async Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Supplier?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Supplier>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.Suppliers.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.TradeName ?? x.LegalName)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Supplier entity, CancellationToken cancellationToken = default)
        => await context.Suppliers.AddAsync(entity, cancellationToken);
}
