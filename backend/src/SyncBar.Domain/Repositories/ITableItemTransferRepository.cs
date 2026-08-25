using SyncBar.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SyncBar.Domain.Repositories
{
    public interface ITableItemTransferRepository
    {
        Task AddAsync(TableItemTransfer entity, CancellationToken cancellationToken = default);
    }
}