using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CompanyRepository(AppDbContext context) : ICompanyRepository
{
    public async Task<Company?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
        => await context.Companies.AsNoTracking().AnyAsync(x => x.Cnpj == cnpj, cancellationToken);

    public async Task AddAsync(Company entity, CancellationToken cancellationToken = default)
        => await context.Companies.AddAsync(entity, cancellationToken);
}
