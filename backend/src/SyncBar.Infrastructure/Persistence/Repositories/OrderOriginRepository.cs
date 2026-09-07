using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class OrderOriginRepository(AppDbContext context) : IOrderOriginRepository
    {
        public async Task<OrderOrigin?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<OrderOrigin>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<OrderOrigin?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<OrderOrigin>()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<IReadOnlyCollection<OrderOrigin>> GetAllAsync(CancellationToken cancellationToken = default)
            => await context.Set<OrderOrigin>()
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<OrderOrigin>> GetByCompanyAndBranchAsync(
            long? companyId, long? branchId, CancellationToken cancellationToken = default)
            => await context.Set<OrderOrigin>()
                .AsNoTracking()
                .Where(x => (x.CompanyId == companyId || x.CompanyId == null) &&
                            (x.BranchId == branchId || x.BranchId == null))
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<OrderOrigin>> GetFilteredAsync(
            long? companyId,
            long? branchId,
            string? searchTerm,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            var query = context.Set<OrderOrigin>().AsNoTracking().AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(x => x.CompanyId == companyId || x.CompanyId == null);
            }

            if (branchId.HasValue)
            {
                query = query.Where(x => x.BranchId == branchId || x.BranchId == null);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(x => EF.Functions.Like(x.Name, $"%{term}%"));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(
            long? companyId, long? branchId, string name, long? excludeId = null, CancellationToken cancellationToken = default)
        {
            var normalizedName = name.Trim().ToUpperInvariant();

            var query = context.Set<OrderOrigin>()
                .AsNoTracking()
                .Where(x => x.Name == normalizedName &&
                            x.CompanyId == companyId &&
                            x.BranchId == branchId);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task AddAsync(OrderOrigin entity, CancellationToken cancellationToken = default)
            => await context.Set<OrderOrigin>().AddAsync(entity, cancellationToken);

        public void Update(OrderOrigin entity)
            => context.Set<OrderOrigin>().Update(entity);

        public void Remove(OrderOrigin entity)
            => context.Set<OrderOrigin>().Remove(entity);
    }
}
