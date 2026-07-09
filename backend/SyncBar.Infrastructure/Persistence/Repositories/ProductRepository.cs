using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Product?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Product>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.Products.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        => await context.Products.AddAsync(entity, cancellationToken);
}
