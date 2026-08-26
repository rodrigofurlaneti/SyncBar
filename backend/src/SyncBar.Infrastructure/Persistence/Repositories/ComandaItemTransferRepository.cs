using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class ComandaItemTransferRepository(AppDbContext context) : IComandaItemTransferRepository
    {
        public async Task AddAsync(ComandaItemTransfer entity, CancellationToken cancellationToken = default)
            => await context.Set<ComandaItemTransfer>().AddAsync(entity, cancellationToken);
    }
}
