using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Category>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.Categories.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Category entity, CancellationToken cancellationToken = default)
        => await context.Categories.AddAsync(entity, cancellationToken);
}
