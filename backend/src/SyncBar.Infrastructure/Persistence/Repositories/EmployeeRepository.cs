using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Employee?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default)
        => await context.Employees.AsNoTracking().AnyAsync(x => x.Cpf == cpf && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<Employee>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.Employees.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Employee entity, CancellationToken cancellationToken = default)
        => await context.Employees.AddAsync(entity, cancellationToken);
}
