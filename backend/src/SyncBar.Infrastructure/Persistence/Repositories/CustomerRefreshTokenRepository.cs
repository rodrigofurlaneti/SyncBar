using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRefreshTokenRepository(AppDbContext context) : ICustomerRefreshTokenRepository
{
    // Tracked — o token e revogado na renovacao.
    public async Task<CustomerRefreshToken?> GetByTokenForUpdateAsync(string token, CancellationToken cancellationToken = default)
        => await context.CustomerRefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

    public async Task AddAsync(CustomerRefreshToken entity, CancellationToken cancellationToken = default)
        => await context.CustomerRefreshTokens.AddAsync(entity, cancellationToken);
}
