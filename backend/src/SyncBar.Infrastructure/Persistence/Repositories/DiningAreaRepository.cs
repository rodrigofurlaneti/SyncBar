using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class DiningAreaRepository : IDiningAreaRepository
    {
        private readonly AppDbContext _context;

        public DiningAreaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DiningArea?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await _context.Set<DiningArea>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<IEnumerable<DiningArea>> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
            => await _context.Set<DiningArea>()
                .AsNoTracking()
                .Where(x => x.BranchId == branchId && x.IsActive)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(DiningArea entity, CancellationToken cancellationToken = default)
            => await _context.Set<DiningArea>().AddAsync(entity, cancellationToken);

        public Task UpdateAsync(DiningArea entity, CancellationToken cancellationToken = default)
        {
            _context.Set<DiningArea>().Update(entity);
            return Task.CompletedTask;
        }
    }
}

