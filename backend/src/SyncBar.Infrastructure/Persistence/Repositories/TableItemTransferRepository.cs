using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class TableItemTransferRepository : ITableItemTransferRepository
    {
        private readonly AppDbContext _context;
        public TableItemTransferRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(TableItemTransfer entity, CancellationToken cancellationToken = default)
        {
            await _context.Set<TableItemTransfer>().AddAsync(entity, cancellationToken);
        }
    }
}
