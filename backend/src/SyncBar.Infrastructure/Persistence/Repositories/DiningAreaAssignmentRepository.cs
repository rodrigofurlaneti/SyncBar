using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class DiningAreaAssignmentRepository(AppDbContext context) : IDiningAreaAssignmentRepository
{
    public async Task<DiningAreaAssignment?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Set<DiningAreaAssignment>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IEnumerable<DiningAreaAssignment>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default)
        => await context.Set<DiningAreaAssignment>()
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.IsActive && x.EndAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<DiningAreaAssignment>> GetActiveByDiningAreaIdAsync(long diningAreaId, CancellationToken cancellationToken = default)
        => await context.Set<DiningAreaAssignment>()
            .AsNoTracking()
            .Where(x => x.DiningAreaId == diningAreaId && x.IsActive && x.EndAt == null)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(DiningAreaAssignment entity, CancellationToken cancellationToken = default)
        => await context.Set<DiningAreaAssignment>().AddAsync(entity, cancellationToken);

    public Task UpdateAsync(DiningAreaAssignment entity, CancellationToken cancellationToken = default)
    {
        context.Set<DiningAreaAssignment>().Update(entity);
        return Task.CompletedTask;
    }
}