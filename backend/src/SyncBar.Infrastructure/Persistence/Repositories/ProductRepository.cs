using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Products.AsNoTracking().Include(x => x.OptionalExtras).Include(x => x.Boosts).AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<Product?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Products.Include(x => x.OptionalExtras).Include(x => x.Boosts).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyCollection<Product>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.Products.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);
    public async Task<IReadOnlyCollection<Product>> GetAllByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.Products.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    public async Task<IReadOnlyCollection<Product>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        => await context.Products.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);
    public async Task<Product?> GetByBarcodeAsync(long companyId, string barcode, CancellationToken cancellationToken = default)
        => await context.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive && x.Barcode == barcode, cancellationToken);
    public async Task<bool> ExistsActiveByCategoryAsync(long categoryId, CancellationToken cancellationToken = default)
        => await context.Products.AsNoTracking()
            .AnyAsync(x => x.CategoryId == categoryId && x.IsActive, cancellationToken);
    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        => await context.Products.AddAsync(entity, cancellationToken);
}
