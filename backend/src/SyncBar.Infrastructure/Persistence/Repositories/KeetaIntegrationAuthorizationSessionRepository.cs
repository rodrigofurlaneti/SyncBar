using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class KeetaIntegrationAuthorizationSessionRepository(AppDbContext context) : IKeetaIntegrationAuthorizationSessionRepository
    {
        public async Task<KeetaIntegrationAuthorizationSession?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationAuthorizationSession>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationAuthorizationSession?> GetByAuthIdAsync(string authId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationAuthorizationSession>()
                .FirstOrDefaultAsync(x => x.AuthId == authId, cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationAuthorizationSession>> GetPendingSessionsAsync(CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationAuthorizationSession>()
                .Where(x => !x.IsProcessed)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(KeetaIntegrationAuthorizationSession session, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationAuthorizationSession>().AddAsync(session, cancellationToken);

        public void Update(KeetaIntegrationAuthorizationSession session)
            => context.Set<KeetaIntegrationAuthorizationSession>().Update(session);

        public void Delete(KeetaIntegrationAuthorizationSession session)
            => context.Set<KeetaIntegrationAuthorizationSession>().Remove(session);
    }
}
